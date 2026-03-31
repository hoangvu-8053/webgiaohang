using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class ReportApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public ReportApiController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // GET: api/ReportApi/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalOrders = await _context.Orders.CountAsync();

            // Lấy % hoa hồng shipper từ cấu hình (mặc định 70%)
            var revenueSettings = _configuration.GetSection("Revenue").Get<RevenueSettings>();
            var shipperCommissionPercent = revenueSettings?.ShipperCommissionPercent ?? 0.7m;
            var platformPercent = 1m - shipperCommissionPercent;

            // Tổng ShippingFee của đơn đã giao
            var totalShippingFee = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => (double)o.ShippingFee);

            // Tổng phí đã trả / sẽ trả cho shipper
            var totalShipperCommission = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => (double)(o.ShippingFee * shipperCommissionPercent));

            // Doanh thu nền tảng thực = ShippingFee - phần shipper
            var totalRevenue = totalShippingFee - totalShipperCommission;

            // Tổng giá trị hàng hóa (không tính vào doanh thu web)
            var totalProductValue = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => (double)(o.Price > 0 ? o.Price : o.TotalAmount - o.ShippingFee));

            var pendingCount = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var shippingCount = await _context.Orders.CountAsync(o => o.Status == "Shipping");
            var deliveredCount = await _context.Orders.CountAsync(o => o.Status == "Delivered");
            var cancelledCount = await _context.Orders.CountAsync(o => o.Status == "Cancelled");

            return Ok(new
            {
                success = true,
                stats = new
                {
                    totalOrders,
                    totalRevenue,
                    totalShippingFee,
                    totalShipperCommission,
                    totalProductValue,
                    platformPercent,
                    pendingCount,
                    shippingCount,
                    deliveredCount,
                    cancelledCount
                }
            });
        }
    }
}
