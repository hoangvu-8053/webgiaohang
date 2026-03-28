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
    public class OrderApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IShippingCalculator _shippingCalculator;
        private readonly IDistanceService _distanceService;
        private readonly INotificationService _notificationService;
        private readonly IShipperPaymentService _shipperPaymentService;
        private readonly IConfiguration _configuration;
        private readonly INominatimGeocodingService _nominatimGeocoding;

        public OrderApiController(
            ApplicationDbContext context,
            IShippingCalculator shippingCalculator,
            IDistanceService distanceService,
            INotificationService notificationService,
            IShipperPaymentService shipperPaymentService,
            IConfiguration configuration,
            INominatimGeocodingService nominatimGeocoding)
        {
            _context = context;
            _shippingCalculator = shippingCalculator;
            _distanceService = distanceService;
            _notificationService = notificationService;
            _shipperPaymentService = shipperPaymentService;
            _configuration = configuration;
            _nominatimGeocoding = nominatimGeocoding;
        }

        // GET: api/OrderApi
        [HttpGet]
        public async Task<IActionResult> GetOrders(string? searchTerm, string? status, int page = 1, int pageSize = 20)
        {
            var username = User.Identity?.Name;
            var query = _context.Orders.AsQueryable();

            if (User.IsInRole("Sender"))
                query = query.Where(o => o.CreatedBy == username);
            else if (User.IsInRole("Receiver"))
                query = query.Where(o => o.ReceiverName == username);
            else if (User.IsInRole("Shipper"))
                query = query.Where(o => o.ShipperName == username);
            else if (!User.IsInRole("Admin") && !User.IsInRole("Staff"))
                return Forbid();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                var term = searchTerm.ToLower();
                query = query.Where(o =>
                    (o.TrackingNumber != null && o.TrackingNumber.ToLower().Contains(term)) ||
                    o.SenderName.ToLower().Contains(term) ||
                    o.ReceiverName.ToLower().Contains(term) ||
                    o.Product.ToLower().Contains(term));
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(o => o.Status == status);

            var totalCount = await query.CountAsync();
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return Ok(new
            {
                success = true,
                orders,
                pagination = new { totalCount, page, pageSize, totalPages = (int)Math.Ceiling(totalCount / (double)pageSize) }
            });
        }

        // GET: api/OrderApi/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(int id)
        {
            var username = User.Identity?.Name;
            var order = await _context.Orders.FindAsync(id);

            if (order == null) return NotFound(new { success = false, message = "Không tìm thấy đơn hàng." });

            bool canAccess = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                            (User.IsInRole("Sender") && order.CreatedBy == username) ||
                            (User.IsInRole("Receiver") && order.ReceiverName == username) ||
                            (User.IsInRole("Shipper") && order.ShipperName == username);

            if (!canAccess) return Forbid();

            return Ok(new { success = true, order });
        }

        // POST: api/OrderApi
        [HttpPost]
        [Authorize(Roles = "Admin,Staff,Sender")]
        public async Task<IActionResult> CreateOrder([FromForm] Order order, IFormFile? productImage)
        {
            order.OrderDate = DateTime.Now;
            order.Status = "Pending";
            order.CreatedBy = User.Identity?.Name ?? "Unknown";
            order.CreatedByRole = User.IsInRole("Sender") ? "Sender" : "Admin";

            if (productImage != null && productImage.Length > 0)
            {
                var fileName = $"order_{DateTime.Now.Ticks}_{productImage.FileName}";
                var path = Path.Combine("wwwroot", "order-images", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await productImage.CopyToAsync(stream);
                order.ProductImagePath = "/order-images/" + fileName;
            }

            try
            {
                var dist = await _distanceService.GetDistanceKmAsync(order.PickupAddress, order.DeliveryAddress);
                if (dist.HasValue) order.DistanceKm = (decimal)dist.Value;
            }
            catch { }

            // Chỉ tin tọa độ lấy từ geocode server (form multipart không gửi lat/lng; tránh client gửi nhầm)
            order.PickupLat = order.PickupLng = order.DeliveryLat = order.DeliveryLng = null;

            // Lưu tọa độ từ địa chỉ chữ — khớp bản đồ với địa chỉ người dùng nhập
            var ct = HttpContext.RequestAborted;
            try
            {
                var pick = await _nominatimGeocoding.GeocodeVietnamAsync(order.PickupAddress, ct);
                if (pick.HasValue)
                {
                    order.PickupLat = pick.Value.lat;
                    order.PickupLng = pick.Value.lng;
                }
                await Task.Delay(1100, ct); // Nominatim: tối đa ~1 request/giây
                var del = await _nominatimGeocoding.GeocodeVietnamAsync(order.DeliveryAddress, ct);
                if (del.HasValue)
                {
                    order.DeliveryLat = del.Value.lat;
                    order.DeliveryLng = del.Value.lng;
                }
            }
            catch { /* mạng / rate limit — đơn vẫn tạo, tọa độ có thể null */ }

            order.ShippingFee = _shippingCalculator.Calculate(order);
            order.TotalAmount = order.Price + order.ShippingFee + (order.InsuranceValue ?? 0);
            order.TrackingNumber = "SHIP" + DateTime.Now.Ticks.ToString().Substring(10);

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                "Đơn hàng mới",
                $"Đơn hàng #{order.Id} vừa được tạo bởi {order.SenderName}",
                null, "Admin,Staff", "Info", "Order", order.Id);

            return Ok(new { success = true, orderId = order.Id, trackingNumber = order.TrackingNumber });
        }

        // POST: api/OrderApi/{id}/status
        [HttpPost("{id}/status")]
        [Authorize(Roles = "Admin,Staff,Shipper")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            if (User.IsInRole("Shipper") && order.ShipperName != User.Identity?.Name) return Forbid();

            var oldStatus = order.Status;
            order.Status = dto.Status;
            if (dto.Status == "Delivered") order.ActualDeliveryDate = DateTime.Now;

            await _context.SaveChangesAsync();

            // Auto-create shipper payment when order is delivered
            if (dto.Status == "Delivered" && !string.IsNullOrEmpty(order.ShipperName))
            {
                try
                {
                    var commissionPercent = 0.7m;
                    var revenueSettings = _configuration.GetSection("Revenue").Get<RevenueSettings>();
                    if (revenueSettings?.ShipperCommissionPercent > 0)
                        commissionPercent = revenueSettings.ShipperCommissionPercent;

                    await _shipperPaymentService.CreateShipperPaymentAsync(order.Id, order.ShipperName, commissionPercent);

                    await _notificationService.CreateNotificationAsync(
                        "Thanh toán cho shipper",
                        $"Đơn hàng #{order.Id} đã giao thành công. Phí shipper: {(order.ShippingFee * commissionPercent):N0} VND",
                        order.ShipperName, null, "Payment", "ShipperPayment", order.Id);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARN] Auto-create shipper payment failed: {ex.Message}");
                }
            }

            await _notificationService.CreateNotificationAsync(
                "Cập nhật trạng thái",
                $"Đơn hàng #{order.Id} đã chuyển sang: {dto.Status}",
                order.SenderName, null, "Info", "Order", order.Id);

            return Ok(new { success = true, message = $"Trạng thái đã cập nhật thành: {dto.Status}" });
        }

        // POST: api/OrderApi/{id}/assign
        [HttpPost("{id}/assign")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AssignShipper(int id, [FromBody] AssignDto dto)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.ShipperName = dto.ShipperName;
            // Giữ nguyên trạng thái Pending để shipper bấm "Lấy hàng"
            // Shipper sẽ tự cập nhật sang "Shipping" khi đi lấy
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(
                "Bạn được gán đơn hàng mới!",
                $"Đơn #{order.Id} - {order.Product} từ {order.PickupAddress} đến {order.DeliveryAddress}. Vui lòng nhận và giao hàng.",
                dto.ShipperName, null, "Order", "Order", order.Id);

            return Ok(new { success = true });
        }

        // GET: api/OrderApi/my-orders
        [HttpGet("my-orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var username = User.Identity?.Name;
            var orders = await _context.Orders
                .Where(o => o.CreatedBy == username || o.ReceiverName == username)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            return Ok(new { success = true, orders });
        }
    }

    public class UpdateStatusDto { public string Status { get; set; } = ""; }
    public class AssignDto { public string ShipperName { get; set; } = ""; }
}
