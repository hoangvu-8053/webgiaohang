using webgiaohang.Models;
using webgiaohang.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace webgiaohang.Services
{
    public interface IPaymentService
    {
        Task<Payment> CreatePaymentAsync(int orderId, string paymentMethod, decimal amount);
        Task<Payment?> GetPaymentByOrderIdAsync(int orderId);
        Task<bool> ProcessPaymentAsync(int paymentId, string transactionId);
        Task<bool> RefundPaymentAsync(int paymentId, string reason);
        Task<List<Payment>> GetUserPaymentsAsync(string username);
        string GeneratePaymentUrl(int paymentId, decimal amount, string returnUrl, string paymentMethod);
        Task<bool> ValidatePaymentAmountAsync(int orderId, decimal paymentAmount);
    }

    public class PaymentService : IPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public PaymentService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        /// <summary>
        /// Tạo payment mới cho đơn hàng
        /// </summary>
        public async Task<Payment> CreatePaymentAsync(int orderId, string paymentMethod, decimal amount)
        {
            // Validate order
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại");

            // Validate payment method
            var validMethods = new[] { "MoMo", "Bank Transfer", "Cash" };
            if (!validMethods.Contains(paymentMethod))
                throw new ArgumentException($"Phương thức thanh toán không hợp lệ: {paymentMethod}");

            // Validate amount
            if (amount <= 0)
                throw new ArgumentException("Số tiền thanh toán phải lớn hơn 0");

            if (amount != order.TotalAmount)
                throw new ArgumentException($"Số tiền thanh toán ({amount:N0} ₫) không khớp với tổng tiền đơn hàng ({order.TotalAmount:N0} ₫)");

            // Kiểm tra xem đã có payment Completed chưa
            var existingCompletedPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status == "Completed");

            if (existingCompletedPayment != null)
                throw new InvalidOperationException($"Đơn hàng #{orderId} đã được thanh toán thành công. Mã thanh toán: #{existingCompletedPayment.Id}");

            // Kiểm tra xem có payment Pending không (cho phép tạo mới nếu payment cũ đã Failed)
            var existingPendingPayment = await _context.Payments
                .FirstOrDefaultAsync(p => p.OrderId == orderId && p.Status == "Pending");

            if (existingPendingPayment != null)
            {
                // Nếu payment cũ đã quá 24 giờ, cho phép tạo mới
                if (existingPendingPayment.CreatedAt.AddHours(24) < DateTime.Now)
                {
                    existingPendingPayment.Status = "Failed";
                    existingPendingPayment.Notes = "Hết hạn thanh toán (quá 24 giờ)";
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Trả về payment pending hiện tại
                    return existingPendingPayment;
                }
            }

            // Tạo payment mới
            var payment = new Payment
            {
                OrderId = orderId,
                PaymentMethod = paymentMethod,
                Amount = amount,
                Status = "Pending",
                CreatedAt = DateTime.Now,
                Notes = $"Thanh toán đơn hàng #{orderId} qua {paymentMethod}"
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            return payment;
        }

        /// <summary>
        /// Lấy payment theo OrderId
        /// </summary>
        public async Task<Payment?> GetPaymentByOrderIdAsync(int orderId)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.OrderId == orderId)
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();
        }

        /// <summary>
        /// Xử lý thanh toán thành công
        /// </summary>
        public async Task<bool> ProcessPaymentAsync(int paymentId, string transactionId)
        {
            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return false;

            // Chỉ xử lý nếu status là Pending
            if (payment.Status != "Pending")
            {
                // Nếu đã Completed rồi, return true (idempotent)
                if (payment.Status == "Completed")
                    return true;
                
                return false;
            }

            // Validate transaction ID
            if (string.IsNullOrWhiteSpace(transactionId))
                throw new ArgumentException("Transaction ID không được để trống");

            // Cập nhật payment
            payment.Status = "Completed";
            payment.TransactionId = transactionId;
            payment.PaidAt = DateTime.Now;
            payment.ReceiptNumber = $"RCP{DateTime.Now:yyyyMMdd}{paymentId:D6}";
            payment.Notes = $"Thanh toán thành công. Transaction ID: {transactionId}";

            // Cập nhật trạng thái đơn hàng nếu cần (chỉ khi đơn hàng đang Pending)
            if (payment.Order != null && payment.Order.Status == "Pending")
            {
                // Không tự động đổi status, để admin/staff xử lý
                // payment.Order.Status = "Processing";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Hoàn tiền (Refund)
        /// </summary>
        public async Task<bool> RefundPaymentAsync(int paymentId, string reason)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Lý do hoàn tiền không được để trống");

            var payment = await _context.Payments
                .Include(p => p.Order)
                .FirstOrDefaultAsync(p => p.Id == paymentId);

            if (payment == null)
                return false;

            // Chỉ có thể refund payment đã Completed
            if (payment.Status != "Completed")
                throw new InvalidOperationException($"Chỉ có thể hoàn tiền cho payment đã thanh toán thành công. Trạng thái hiện tại: {payment.Status}");

            // Kiểm tra xem đã refund chưa
            if (payment.Status == "Refunded")
                throw new InvalidOperationException("Payment này đã được hoàn tiền rồi");

            // Cập nhật status
            payment.Status = "Refunded";
            payment.Notes = $"Đã hoàn tiền. Lý do: {reason}. Thời gian: {DateTime.Now:dd/MM/yyyy HH:mm}";

            // Cập nhật trạng thái đơn hàng nếu cần
            if (payment.Order != null)
            {
                // Có thể đổi status đơn hàng thành "Cancelled" hoặc giữ nguyên
                // payment.Order.Status = "Cancelled";
            }

            await _context.SaveChangesAsync();
            return true;
        }

        /// <summary>
        /// Lấy danh sách payment của user
        /// </summary>
        public async Task<List<Payment>> GetUserPaymentsAsync(string username)
        {
            return await _context.Payments
                .Include(p => p.Order)
                .Where(p => p.Order != null && 
                    (p.Order.CreatedBy == username || p.Order.SenderName == username))
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Tạo payment URL (không còn sử dụng - đã xóa VNPay và ZaloPay)
        /// </summary>
        public string GeneratePaymentUrl(int paymentId, decimal amount, string returnUrl, string paymentMethod)
        {
            throw new ArgumentException($"Payment method {paymentMethod} không hỗ trợ payment gateway URL");
        }

        /// <summary>
        /// Validate payment amount với order total
        /// </summary>
        public async Task<bool> ValidatePaymentAmountAsync(int orderId, decimal paymentAmount)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return false;

            return Math.Abs(paymentAmount - order.TotalAmount) < 0.01m; // Cho phép sai số 0.01 VND
        }

        /// <summary>
        /// Tính HMAC SHA512 cho VNPay
        /// </summary>
        private string ComputeHMACSHA512(string key, string data)
        {
            using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(key));
            var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }

        /// <summary>
        /// Lấy IP address của client (mock - trong production cần lấy từ HttpContext)
        /// </summary>
        private string GetClientIpAddress()
        {
            // Trong production, inject IHttpContextAccessor và lấy IP thực tế
            return "127.0.0.1";
        }
    }
}
