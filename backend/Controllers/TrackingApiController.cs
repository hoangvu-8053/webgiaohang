using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TrackingApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TrackingApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/TrackingApi/{trackingNumber}
        [HttpGet("{trackingNumber}")]
        public async Task<IActionResult> GetTracking(string trackingNumber)
        {
            var order = await _context.Orders
                .Where(o => o.TrackingNumber == trackingNumber)
                .Select(o => new
                {
                    o.Id,
                    o.TrackingNumber,
                    o.Status,
                    o.PickupAddress,
                    o.PickupLat,
                    o.PickupLng,
                    o.DeliveryAddress,
                    o.DeliveryLat,
                    o.DeliveryLng,
                    o.DistanceKm,
                    o.OrderDate,
                    o.ActualDeliveryDate,
                    o.ShipperName,
                    o.ShipperLat,
                    o.ShipperLng,
                    o.ShipperLocationUpdatedAt,
                    o.Product
                })
                .FirstOrDefaultAsync();

            if (order == null) return NotFound(new { success = false, message = "Không tìm thấy mã vận đơn." });

            return Ok(new { success = true, order });
        }

        // GET: api/TrackingApi/map/{orderId}
        [HttpGet("map/{orderId}")]
        public async Task<IActionResult> GetOrderMap(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null) return NotFound();

            return Ok(new
            {
                success = true,
                order = new
                {
                    order.Id,
                    order.PickupAddress,
                    order.PickupLat,
                    order.PickupLng,
                    order.DeliveryAddress,
                    order.DeliveryLat,
                    order.DeliveryLng,
                    order.ShipperLat,
                    order.ShipperLng,
                    order.Status
                }
            });
        }
    }
}
