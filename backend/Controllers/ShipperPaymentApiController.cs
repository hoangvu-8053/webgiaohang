using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;
using webgiaohang.Services;
using System.Security.Claims;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ShipperPaymentApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IShipperPaymentService _shipperPaymentService;
        private readonly IConfiguration _configuration;

        public ShipperPaymentApiController(
            ApplicationDbContext context,
            IShipperPaymentService shipperPaymentService,
            IConfiguration configuration)
        {
            _context = context;
            _shipperPaymentService = shipperPaymentService;
            _configuration = configuration;
        }

        // ============================================
        // LẤY DANH SÁCH THANH TOÁN CỦA SHIPPER
        // GET: api/ShipperPaymentApi
        // ============================================
        [HttpGet]
        public async Task<IActionResult> GetShipperPayments([FromQuery] string? status = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized(new { success = false, message = "Không xác thực được người dùng" });

            var query = _context.ShipperPayments
                .Include(sp => sp.Order)
                .Where(sp => sp.ShipperName == username)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(sp => sp.Status == status);

            var payments = await query
                .OrderByDescending(sp => sp.CreatedAt)
                .Select(sp => new
                {
                    sp.Id,
                    sp.OrderId,
                    OrderTrackingNumber = sp.Order!.TrackingNumber,
                    OrderProduct = sp.Order.Product,
                    sp.Amount,
                    sp.CommissionPercent,
                    sp.OrderTotalAmount,
                    sp.Status,
                    sp.PaidAt,
                    sp.PaymentMethod,
                    sp.TransactionId,
                    sp.CreatedAt
                })
                .ToListAsync();

            // Thống kê
            var stats = new
            {
                pendingCount = await _context.ShipperPayments.CountAsync(sp => sp.ShipperName == username && sp.Status == "Pending"),
                paidCount = await _context.ShipperPayments.CountAsync(sp => sp.ShipperName == username && sp.Status == "Paid"),
                pendingAmount = await _shipperPaymentService.GetPendingAmountAsync(username),
                paidAmount = await _shipperPaymentService.GetPaidAmountAsync(username)
            };

            return Ok(new { success = true, payments, stats });
        }

        // ============================================
        // LẤY THÔNG TIN THANH TOÁN THEO ID
        // GET: api/ShipperPaymentApi/{id}
        // ============================================
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPayment(int id)
        {
            var username = User.Identity?.Name;
            var payment = await _context.ShipperPayments
                .Include(sp => sp.Order)
                .FirstOrDefaultAsync(sp => sp.Id == id);

            if (payment == null)
                return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

            // Kiểm tra quyền (shipper sở hữu hoặc admin)
            if (payment.ShipperName != username && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
                return Forbid();

            return Ok(new
            {
                success = true,
                payment = new
                {
                    payment.Id,
                    payment.OrderId,
                    OrderTrackingNumber = payment.Order!.TrackingNumber,
                    OrderProduct = payment.Order.Product,
                    payment.Amount,
                    payment.CommissionPercent,
                    payment.OrderTotalAmount,
                    payment.Status,
                    payment.PaidAt,
                    payment.PaymentMethod,
                    payment.TransactionId,
                    payment.Notes,
                    payment.CreatedAt
                }
            });
        }

        // ============================================
        // XÁC NHẬN THANH TOÁN CHO SHIPPER (ADMIN)
        // POST: api/ShipperPaymentApi/{id}/pay
        // ============================================
        [HttpPost("{id}/pay")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> MarkAsPaid(int id, [FromBody] MarkAsPaidRequest request)
        {
            var payment = await _context.ShipperPayments.FindAsync(id);
            if (payment == null)
                return NotFound(new { success = false, message = "Không tìm thấy thanh toán" });

            if (payment.Status == "Paid")
                return BadRequest(new { success = false, message = "Thanh toán này đã được xác nhận" });

            payment.Status = "Paid";
            payment.PaidAt = DateTime.Now;
            payment.PaymentMethod = request.PaymentMethod ?? "Cash";
            payment.TransactionId = request.TransactionId ?? $"SP-{DateTime.Now:yyyyMMddHHmmss}-{id}";
            payment.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            // Gửi thông báo cho shipper
            var notification = new Notification
            {
                Title = "Đã thanh toán cho shipper!",
                Message = $"Đơn hàng #{payment.OrderId} - Số tiền: {payment.Amount:N0} VND đã được thanh toán qua {payment.PaymentMethod}",
                RecipientUsername = payment.ShipperName,
                Type = "Payment",
                RelatedEntityType = "ShipperPayment",
                RelatedEntityId = payment.Id,
                CreatedAt = DateTime.Now
            };
            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Xác nhận thanh toán thành công",
                payment = new
                {
                    payment.Id,
                    payment.Status,
                    payment.PaidAt,
                    payment.PaymentMethod,
                    payment.TransactionId
                }
            });
        }

        // ============================================
        // LẤY TẤT CẢ THANH TOÁN CHƯA TRẢ (ADMIN)
        // GET: api/ShipperPaymentApi/pending
        // ============================================
        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPendingPayments()
        {
            var payments = await _shipperPaymentService.GetPendingPaymentsAsync();

            return Ok(new
            {
                success = true,
                payments = payments.Select(sp => new
                {
                    sp.Id,
                    sp.OrderId,
                    sp.ShipperName,
                    OrderTrackingNumber = sp.Order!.TrackingNumber,
                    OrderProduct = sp.Order.Product,
                    sp.Amount,
                    sp.CommissionPercent,
                    sp.OrderTotalAmount,
                    sp.Status,
                    sp.CreatedAt
                }),
                summary = new
                {
                    count = payments.Count,
                    totalAmount = payments.Sum(p => p.Amount)
                }
            });
        }

        // ============================================
        // TẠO THANH TOÁN KHI GIAO HÀNG THÀNH CÔNG (AUTO)
        // POST: api/ShipperPaymentApi/create-for-order/{orderId}
        // ============================================
        [HttpPost("create-for-order/{orderId}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CreateForOrder(int orderId)
        {
            try
            {
                var order = await _context.Orders.FindAsync(orderId);
                if (order == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

                if (string.IsNullOrEmpty(order.ShipperName))
                    return BadRequest(new { success = false, message = "Don hang chua co shipper" });

                var commissionPercent = 0.7m; // 70%
                var revenueSettings = _configuration.GetSection("Revenue").Get<RevenueSettings>();
                if (revenueSettings?.ShipperCommissionPercent > 0)
                    commissionPercent = revenueSettings.ShipperCommissionPercent;

                var payment = await _shipperPaymentService.CreateShipperPaymentAsync(orderId, order.ShipperName, commissionPercent);

                return Ok(new
                {
                    success = true,
                    message = "Tạo thanh toán cho shipper thành công",
                    payment = new
                    {
                        payment.Id,
                        payment.Amount,
                        payment.CommissionPercent,
                        payment.Status
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        // ============================================
        // THỐNG KÊ TẤT CẢ THANH TOÁN SHIPPER (ADMIN)
        // GET: api/ShipperPaymentApi/stats
        // ============================================
        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetStats()
        {
            var allPayments = await _context.ShipperPayments.ToListAsync();

            var stats = new
            {
                totalPayments = allPayments.Count,
                pendingCount = allPayments.Count(p => p.Status == "Pending"),
                paidCount = allPayments.Count(p => p.Status == "Paid"),
                totalPendingAmount = allPayments.Where(p => p.Status == "Pending").Sum(p => p.Amount),
                totalPaidAmount = allPayments.Where(p => p.Status == "Paid").Sum(p => p.Amount),
                byShipper = allPayments
                    .GroupBy(p => p.ShipperName)
                    .Select(g => new
                    {
                        shipperName = g.Key,
                        pendingCount = g.Count(p => p.Status == "Pending"),
                        paidCount = g.Count(p => p.Status == "Paid"),
                        pendingAmount = g.Where(p => p.Status == "Pending").Sum(p => p.Amount),
                        paidAmount = g.Where(p => p.Status == "Paid").Sum(p => p.Amount)
                    })
                    .OrderByDescending(s => s.pendingAmount)
                    .ToList()
            };

            return Ok(new { success = true, stats });
        }
    }

    public class MarkAsPaidRequest
    {
        public string? PaymentMethod { get; set; }
        public string? TransactionId { get; set; }
    }
}
