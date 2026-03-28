using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin,Staff")]
    public class ReportApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ReportApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ReportApi/summary
        [HttpGet("summary")]
        public async Task<IActionResult> GetSummary()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalRevenue = await _context.Orders
                .Where(o => o.Status == "Delivered")
                .SumAsync(o => (double)o.TotalAmount);

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
                    pendingCount,
                    shippingCount,
                    deliveredCount,
                    cancelledCount
                }
            });
        }
    }
}
