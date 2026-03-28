using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using webgiaohang.Data;
using webgiaohang.Models;
using webgiaohang.Hubs;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LiveMapApiController : ControllerBase
    {
        private const string DualAuth = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme;
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<LocationHub> _locationHub;

        public LiveMapApiController(ApplicationDbContext context, IHubContext<LocationHub> locationHub)
        {
            _context = context;
            _locationHub = locationHub;
        }

        // ============================================
        // LẤY THÔNG TIN MAP CHO ORDER
        // GET: api/LiveMapApi/order/{orderId}
        // ============================================
        [HttpGet("order/{orderId}")]
        public async Task<IActionResult> GetOrderMapInfo(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            return Ok(new
            {
                success = true,
                order = new
                {
                    order.Id,
                    order.TrackingNumber,
                    order.Status,
                    order.ShipperName,
                    order.PickupAddress,
                    order.PickupLat,
                    order.PickupLng,
                    order.DeliveryAddress,
                    order.DeliveryLat,
                    order.DeliveryLng,
                    order.ShipperLat,
                    order.ShipperLng,
                    order.ShipperLocationUpdatedAt,
                    order.CurrentLocation,
                    order.Product,
                    order.Price,
                    order.ShippingFee,
                    order.TotalAmount
                }
            });
        }

        // ============================================
        // CẬP NHẬT VỊ TRÍ SHIPPER (từ app mobile)
        // POST: api/LiveMapApi/shipper/location
        // ============================================
        [HttpPost("shipper/location")]
        [Authorize(Roles = "Shipper", AuthenticationSchemes = DualAuth)]
        public async Task<IActionResult> UpdateShipperLocation([FromBody] UpdateShipperLocationRequest request)
        {
            var username = User.Identity?.Name;

            // Tìm đơn hàng đang giao của shipper
            var activeOrder = await _context.Orders
                .FirstOrDefaultAsync(o => o.ShipperName == username &&
                    (o.Status == "Shipping" || o.Status == "Pending"));

            if (activeOrder == null)
            {
                return NotFound(new { success = false, message = "Không có đơn hàng đang giao" });
            }

            // Cập nhật vị trí
            activeOrder.ShipperLat = request.Latitude;
            activeOrder.ShipperLng = request.Longitude;
            activeOrder.ShipperLocationUpdatedAt = DateTime.Now;
            activeOrder.CurrentLocation = request.Address;

            await _context.SaveChangesAsync();

            // Broadcast location update qua SignalR
            await _locationHub.Clients.Group($"order_{activeOrder.Id}").SendAsync("LocationUpdated", new
            {
                orderId = activeOrder.Id,
                lat = request.Latitude,
                lng = request.Longitude,
                address = request.Address,
                timestamp = DateTime.Now,
                shipperName = username
            });

            return Ok(new
            {
                success = true,
                message = "Cập nhật vị trí thành công",
                location = new
                {
                    latitude = request.Latitude,
                    longitude = request.Longitude,
                    address = request.Address,
                    timestamp = DateTime.Now
                }
            });
        }

        // ============================================
        // LẤY VỊ TRÍ TẤT CẢ SHIPPER ĐANG HOẠT ĐỘNG
        // GET: api/LiveMapApi/shippers/active
        // ============================================
        [HttpGet("shippers/active")]
        [Authorize(Roles = "Admin,Staff", AuthenticationSchemes = DualAuth)]
        public async Task<IActionResult> GetActiveShippers()
        {
            // Lấy các đơn đang Shipping để biết shipper đang hoạt động
            var activeOrders = await _context.Orders
                .Where(o => o.Status == "Shipping" && o.ShipperLat != null && o.ShipperLng != null)
                .Select(o => new
                {
                    o.ShipperName,
                    o.Id,
                    o.TrackingNumber,
                    o.ShipperLat,
                    o.ShipperLng,
                    o.ShipperLocationUpdatedAt,
                    o.PickupAddress,
                    o.DeliveryAddress,
                    o.Product
                })
                .ToListAsync();

            // Nhóm theo shipper (lấy đơn gần nhất)
            var shipperLocations = activeOrders
                .GroupBy(o => o.ShipperName)
                .Select(g => g.OrderByDescending(o => o.ShipperLocationUpdatedAt).First())
                .Select(o => new
                {
                    shipperName = o.ShipperName,
                    orderId = o.Id,
                    trackingNumber = o.TrackingNumber,
                    latitude = o.ShipperLat,
                    longitude = o.ShipperLng,
                    lastUpdated = o.ShipperLocationUpdatedAt,
                    currentAddress = o.PickupAddress,
                    destination = o.DeliveryAddress,
                    product = o.Product
                })
                .ToList();

            return Ok(new { success = true, shippers = shipperLocations });
        }

        // ============================================
        // THEO DÕI VỊ TRÍ SHIPPER THEO ORDER (cho sender/receiver)
        // GET: api/LiveMapApi/track/{orderId}
        // ============================================
        [HttpGet("track/{orderId}")]
        [Authorize(AuthenticationSchemes = DualAuth)]
        public async Task<IActionResult> TrackShipper(int orderId)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound(new { success = false, message = "Khong tim thay don hang" });

            // Kiểm tra quyền
            var username = User.Identity?.Name;
            bool canTrack = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                           order.CreatedBy == username ||
                           order.SenderName == username ||
                           order.ReceiverName == username ||
                           order.ShipperName == username;

            if (!canTrack)
                return Forbid();

            // Lấy thông tin shipper từ bảng Users
            User? shipper = null;
            if (!string.IsNullOrEmpty(order.ShipperName))
            {
                shipper = await _context.Users.FirstOrDefaultAsync(u => u.Username == order.ShipperName);
            }

            // Tính khoảng cách từ shipper đến điểm giao
            double? distanceToDelivery = null;
            if (order.ShipperLat.HasValue && order.ShipperLng.HasValue &&
                order.DeliveryLat.HasValue && order.DeliveryLng.HasValue)
            {
                distanceToDelivery = CalculateDistance(
                    (double)order.ShipperLat.Value, (double)order.ShipperLng.Value,
                    (double)order.DeliveryLat.Value, (double)order.DeliveryLng.Value);
            }

            return Ok(new
            {
                success = true,
                tracking = new
                {
                    id = order.Id,
                    orderId = order.Id,
                    trackingNumber = order.TrackingNumber,
                    status = order.Status,
                    shipper = shipper != null ? new
                    {
                        name = shipper.FullName ?? shipper.Username,
                        phone = shipper.Phone
                    } : null,
                    pickupAddress = order.PickupAddress,
                    pickupLat = order.PickupLat,
                    pickupLng = order.PickupLng,
                    deliveryAddress = order.DeliveryAddress,
                    deliveryLat = order.DeliveryLat,
                    deliveryLng = order.DeliveryLng,
                    shipperLat = order.ShipperLat,
                    shipperLng = order.ShipperLng,
                    currentLocation = order.CurrentLocation,
                    shipperLocationUpdatedAt = order.ShipperLocationUpdatedAt,
                    shipperLocation = order.ShipperLat.HasValue && order.ShipperLng.HasValue ? new
                    {
                        lat = order.ShipperLat,
                        lng = order.ShipperLng,
                        address = order.CurrentLocation,
                        lastUpdated = order.ShipperLocationUpdatedAt,
                        distanceToDeliveryKm = distanceToDelivery
                    } : null,
                    estimatedArrival = EstimateArrival(distanceToDelivery)
                }
            });
        }

        // ============================================
        // LẤY LỘ TRÌNH CỦA SHIPPER
        // GET: api/LiveMapApi/route/{orderId}
        // ============================================
        [HttpGet("route/{orderId}")]
        [Authorize(AuthenticationSchemes = DualAuth)]
        public async Task<IActionResult> GetShipperRoute(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            // Kiểm tra quyền
            var username = User.Identity?.Name;
            bool canTrack = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                           order.CreatedBy == username ||
                           order.SenderName == username ||
                           order.ReceiverName == username ||
                           order.ShipperName == username;

            if (!canTrack)
                return Forbid();

            // Tạo route points (pickup -> shipper -> delivery)
            var routePoints = new List<object>();

            // Điểm lấy hàng
            if (order.PickupLat.HasValue && order.PickupLng.HasValue)
            {
                routePoints.Add(new
                {
                    type = "pickup",
                    lat = order.PickupLat,
                    lng = order.PickupLng,
                    address = order.PickupAddress,
                    label = "A - Điểm lấy hàng"
                });
            }

            // Vị trí shipper hiện tại
            if (order.ShipperLat.HasValue && order.ShipperLng.HasValue)
            {
                routePoints.Add(new
                {
                    type = "shipper",
                    lat = order.ShipperLat,
                    lng = order.ShipperLng,
                    address = order.CurrentLocation ?? "Vị trí hiện tại",
                    label = "Shipper",
                    lastUpdated = order.ShipperLocationUpdatedAt
                });
            }

            // Điểm giao hàng
            if (order.DeliveryLat.HasValue && order.DeliveryLng.HasValue)
            {
                routePoints.Add(new
                {
                    type = "delivery",
                    lat = order.DeliveryLat,
                    lng = order.DeliveryLng,
                    address = order.DeliveryAddress,
                    label = "B - Điểm giao hàng"
                });
            }

            // Tính tổng khoảng cách
            double? totalDistance = null;
            if (order.ShipperLat.HasValue && order.ShipperLng.HasValue)
            {
                if (order.PickupLat.HasValue && order.PickupLng.HasValue)
                {
                    totalDistance = CalculateDistance(
                        (double)order.PickupLat.Value, (double)order.PickupLng.Value,
                        (double)order.ShipperLat.Value, (double)order.ShipperLng.Value);
                }
                if (order.DeliveryLat.HasValue && order.DeliveryLng.HasValue)
                {
                    var deliveryDist = CalculateDistance(
                        (double)order.ShipperLat.Value, (double)order.ShipperLng.Value,
                        (double)order.DeliveryLat.Value, (double)order.DeliveryLng.Value);
                    totalDistance = (totalDistance ?? 0) + deliveryDist;
                }
            }

            return Ok(new
            {
                success = true,
                route = new
                {
                    orderId = order.Id,
                    trackingNumber = order.TrackingNumber,
                    status = order.Status,
                    points = routePoints,
                    totalDistanceKm = totalDistance,
                    googleMapsUrl = GenerateGoogleMapsUrl(order)
                }
            });
        }

        // ============================================
        // ĐĂNG KÝ THEO DÕI VỊ TRÍ (SignalR subscription)
        // POST: api/LiveMapApi/subscribe/{orderId}
        // ============================================
        [HttpPost("subscribe/{orderId}")]
        [Authorize(AuthenticationSchemes = DualAuth)]
        public async Task<IActionResult> SubscribeToOrder(int orderId)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order == null)
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });

            // Kiểm tra quyền
            var username = User.Identity?.Name;
            bool canSubscribe = User.IsInRole("Admin") || User.IsInRole("Staff") ||
                              order.CreatedBy == username ||
                              order.SenderName == username ||
                              order.ReceiverName == username;

            if (!canSubscribe)
                return Forbid();

            // Thông tin để client kết nối SignalR
            return Ok(new
            {
                success = true,
                message = "Đăng ký theo dõi thành công",
                connection = new
                {
                    hubUrl = "/locationHub",
                    groupName = $"order_{orderId}",
                    methodName = "LocationUpdated",
                    orderId = orderId
                }
            });
        }

        // ============================================
        // LẤY TẤT CẢ ĐƠN HÀNG CÓ VỊ TRÍ ĐỂ HIỂN THỊ TRÊN MAP (Admin)
        // GET: api/LiveMapApi/all-orders
        // ============================================
        [HttpGet("all-orders")]
        [Authorize(Roles = "Admin,Staff", AuthenticationSchemes = DualAuth)]
        public async Task<IActionResult> GetAllOrdersWithLocation()
        {
            var orders = await _context.Orders
                .Where(o => o.ShipperLat != null && o.ShipperLng != null)
                .Where(o => o.Status == "Shipping" || o.Status == "Pending")
                .Select(o => new
                {
                    o.Id,
                    o.TrackingNumber,
                    o.Status,
                    o.ShipperName,
                    o.Product,
                    o.PickupAddress,
                    o.PickupLat,
                    o.PickupLng,
                    o.DeliveryAddress,
                    o.DeliveryLat,
                    o.DeliveryLng,
                    o.ShipperLat,
                    o.ShipperLng,
                    o.ShipperLocationUpdatedAt
                })
                .ToListAsync();

            return Ok(new { success = true, orders });
        }

        // ============================================
        // Helper Methods
        // ============================================

        // Tính khoảng cách Haversine (km)
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371; // Bán kính Trái Đất (km)
            var dLat = ToRadians(lat2 - lat1);
            var dLon = ToRadians(lon2 - lon1);
            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return Math.Round(R * c, 2);
        }

        private double ToRadians(double degree)
        {
            return degree * Math.PI / 180;
        }

        // Ước tính thời gian đến (km -> phút, giả định 30km/h)
        private string EstimateArrival(double? distanceKm)
        {
            if (!distanceKm.HasValue || distanceKm.Value <= 0)
                return "Đang tính...";

            var hours = distanceKm.Value / 30.0;
            var minutes = (int)(hours * 60);

            if (minutes < 1) return "Sắp đến";
            if (minutes < 60) return $"{minutes} phút";
            return $"{minutes / 60} giờ {minutes % 60} phút";
        }

        // Tạo Google Maps URL
        private string GenerateGoogleMapsUrl(Order order)
        {
            var baseUrl = "https://www.google.com/maps/dir/";
            var points = new List<string>();

            if (order.PickupLat.HasValue && order.PickupLng.HasValue)
                points.Add($"{order.PickupLat},{order.PickupLng}");

            if (order.ShipperLat.HasValue && order.ShipperLng.HasValue)
                points.Add($"{order.ShipperLat},{order.ShipperLng}");

            if (order.DeliveryLat.HasValue && order.DeliveryLng.HasValue)
                points.Add($"{order.DeliveryLat},{order.DeliveryLng}");

            return points.Count >= 2 ? baseUrl + string.Join("/", points) : "";
        }
    }

    public class UpdateShipperLocationRequest
    {
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public string? Address { get; set; }
    }
}
