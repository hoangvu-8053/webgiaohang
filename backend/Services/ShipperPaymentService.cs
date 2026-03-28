using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;

namespace webgiaohang.Services
{
    public class ShipperPaymentService : IShipperPaymentService
    {
        private readonly ApplicationDbContext _context;

        public ShipperPaymentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<decimal> CalculateShipperEarningAsync(int orderId, decimal commissionPercent)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại");

            // Shipper nhận được phần trăm của ShippingFee
            // Ví dụ: commissionPercent = 0.7 (70%) thì shipper nhận 70% của ShippingFee
            var shipperEarning = order.ShippingFee * commissionPercent;
            
            // Làm tròn đến 1000đ
            shipperEarning = Math.Ceiling(shipperEarning / 1000m) * 1000m;

            return shipperEarning;
        }

        public async Task<ShipperPayment> CreateShipperPaymentAsync(int orderId, string shipperName, decimal commissionPercent)
        {
            // Kiểm tra xem đã có thanh toán cho đơn hàng này chưa
            var existingPayment = await _context.ShipperPayments
                .FirstOrDefaultAsync(sp => sp.OrderId == orderId && sp.ShipperName == shipperName);

            if (existingPayment != null)
            {
                // Nếu đã có và đã thanh toán, không tạo mới
                if (existingPayment.Status == "Paid")
                {
                    return existingPayment;
                }
                // Nếu đang Pending, cập nhật lại
                existingPayment.Amount = await CalculateShipperEarningAsync(orderId, commissionPercent);
                existingPayment.CommissionPercent = commissionPercent;
                existingPayment.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return existingPayment;
            }

            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                throw new ArgumentException("Đơn hàng không tồn tại");

            if (string.IsNullOrEmpty(shipperName))
                throw new ArgumentException("Tên shipper không được để trống");

            var amount = await CalculateShipperEarningAsync(orderId, commissionPercent);

            var shipperPayment = new ShipperPayment
            {
                OrderId = orderId,
                ShipperName = shipperName,
                Amount = amount,
                CommissionPercent = commissionPercent,
                OrderTotalAmount = order.TotalAmount,
                Status = "Pending",
                CreatedAt = DateTime.Now
            };

            _context.ShipperPayments.Add(shipperPayment);
            await _context.SaveChangesAsync();

            return shipperPayment;
        }

        public async Task<List<ShipperPayment>> GetShipperPaymentsAsync(string shipperName)
        {
            return await _context.ShipperPayments
                .Include(sp => sp.Order)
                .Where(sp => sp.ShipperName == shipperName)
                .OrderByDescending(sp => sp.CreatedAt)
                .ToListAsync();
        }

        public async Task<ShipperPayment?> GetShipperPaymentByIdAsync(int paymentId)
        {
            return await _context.ShipperPayments
                .Include(sp => sp.Order)
                .FirstOrDefaultAsync(sp => sp.Id == paymentId);
        }

        public async Task<ShipperPayment?> GetShipperPaymentByOrderIdAsync(int orderId)
        {
            return await _context.ShipperPayments
                .Include(sp => sp.Order)
                .FirstOrDefaultAsync(sp => sp.OrderId == orderId);
        }

        public async Task<bool> MarkAsPaidAsync(int paymentId, string paymentMethod, string? transactionId = null)
        {
            var payment = await _context.ShipperPayments.FindAsync(paymentId);
            if (payment == null)
                return false;

            if (payment.Status == "Paid")
                return true; // Đã thanh toán rồi

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;
            payment.PaymentMethod = paymentMethod;
            payment.TransactionId = transactionId;
            payment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<decimal> GetPendingAmountAsync(string shipperName)
        {
            return await _context.ShipperPayments
                .Where(sp => sp.ShipperName == shipperName && sp.Status == "Pending")
                .SumAsync(sp => sp.Amount);
        }

        public async Task<decimal> GetPaidAmountAsync(string shipperName)
        {
            return await _context.ShipperPayments
                .Where(sp => sp.ShipperName == shipperName && sp.Status == "Paid")
                .SumAsync(sp => sp.Amount);
        }

        public async Task<List<ShipperPayment>> GetPendingPaymentsAsync()
        {
            return await _context.ShipperPayments
                .Include(sp => sp.Order)
                .Where(sp => sp.Status == "Pending")
                .OrderBy(sp => sp.CreatedAt)
                .ToListAsync();
        }
    }
}

