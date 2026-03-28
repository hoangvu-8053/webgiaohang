using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using webgiaohang.Data;
using webgiaohang.Models;
using webgiaohang.Hubs;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShipperApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<LocationHub> _locationHub;

        public ShipperApiController(ApplicationDbContext context, IConfiguration configuration, IHubContext<LocationHub> locationHub)
        {
            _context = context;
            _configuration = configuration;
            _locationHub = locationHub;
        }

        // POST: api/ShipperApi/Login
        [HttpPost("login")]
        [AllowAnonymous]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập đầy đủ thông tin" });
            }

            var user = _context.Users.FirstOrDefault(u => u.Username == request.Username);
            if (user == null || user.PasswordHash != ComputeHash(request.Password))
            {
                return Unauthorized(new { success = false, message = "Sai tên đăng nhập hoặc mật khẩu" });
            }

            if (!user.IsApproved)
            {
                return Unauthorized(new { success = false, message = "Tài khoản chưa được phê duyệt" });
            }

            if (user.Role != "Shipper")
            {
                return StatusCode(403, new { success = false, message = "Chỉ shipper mới có thể đăng nhập" });
            }

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                success = true,
                token = token,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.Phone,
                    role = user.Role
                }
            });
        }

        private const string ShipperAuthSchemes = CookieAuthenticationDefaults.AuthenticationScheme + "," + JwtBearerDefaults.AuthenticationScheme;

        // GET: api/ShipperApi/Orders
        [HttpGet("orders")]
        [Authorize(Roles = "Shipper", AuthenticationSchemes = ShipperAuthSchemes)]
        public async Task<IActionResult> GetOrders([FromQuery] string? status = null)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                return Unauthorized(new { success = false, message = "Không xác thực được người dùng" });
            }

            var query = _context.Orders
                .Where(o => o.ShipperName == username)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(o => o.Status == status);
            }
            else
            {
                // Mặc định chỉ lấy đơn hàng đang xử lý (chưa giao xong)
                query = query.Where(o => o.Status != "Delivered" && o.Status != "Cancelled" && o.Status != "Failed");
            }

            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Select(o => new
                {
                    id = o.Id,
                    trackingNumber = o.TrackingNumber,
                    product = o.Product,
                    senderName = o.SenderName,
                    senderPhone = o.SenderPhone,
                    senderAddress = o.PickupAddress,
                    receiverName = o.ReceiverName,
                    receiverPhone = o.ReceiverPhone,
                    receiverAddress = o.DeliveryAddress,
                    status = o.Status,
                    orderDate = o.OrderDate,
                    estimatedDeliveryDate = o.EstimatedDeliveryDate,
                    actualDeliveryDate = o.ActualDeliveryDate,
                    distanceKm = o.DistanceKm,
                    shippingFee = o.ShippingFee,
                    totalAmount = o.TotalAmount,
                    pickupLat = o.PickupLat,
                    pickupLng = o.PickupLng,
                    deliveryLat = o.DeliveryLat,
                    deliveryLng = o.DeliveryLng,
                    shipperLat = o.ShipperLat,
                    shipperLng = o.ShipperLng,
                    notes = o.Notes,
                    productImagePath = o.ProductImagePath
                })
                .ToListAsync();

            return Ok(new { success = true, orders = orders });
        }

        // GET: api/ShipperApi/Orders/{id}
        [HttpGet("orders/{id}")]
        [Authorize(Roles = "Shipper", AuthenticationSchemes = ShipperAuthSchemes)]
        public async Task<IActionResult> GetOrder(int id)
        {
            var username = User.Identity?.Name;
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.ShipperName == username);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            return Ok(new
            {
                success = true,
                order = new
                {
                    id = order.Id,
                    trackingNumber = order.TrackingNumber,
                    product = order.Product,
                    senderName = order.SenderName,
                    senderPhone = order.SenderPhone,
                    senderEmail = order.SenderEmail,
                    senderAddress = order.PickupAddress,
                    receiverName = order.ReceiverName,
                    receiverPhone = order.ReceiverPhone,
                    receiverEmail = order.ReceiverEmail,
                    receiverAddress = order.DeliveryAddress,
                    status = order.Status,
                    orderDate = order.OrderDate,
                    estimatedDeliveryDate = order.EstimatedDeliveryDate,
                    actualDeliveryDate = order.ActualDeliveryDate,
                    distanceKm = order.DistanceKm,
                    shippingFee = order.ShippingFee,
                    totalAmount = order.TotalAmount,
                    pickupLat = order.PickupLat,
                    pickupLng = order.PickupLng,
                    deliveryLat = order.DeliveryLat,
                    deliveryLng = order.DeliveryLng,
                    shipperLat = order.ShipperLat,
                    shipperLng = order.ShipperLng,
                    notes = order.Notes,
                    productImagePath = order.ProductImagePath
                }
            });
        }

        // POST: api/ShipperApi/Orders/{id}/UpdateStatus
        [HttpPost("orders/{id}/status")]
        [Authorize(Roles = "Shipper", AuthenticationSchemes = ShipperAuthSchemes)]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateStatusRequest request)
        {
            var username = User.Identity?.Name;
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.ShipperName == username);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            if (string.IsNullOrEmpty(request.Status))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập trạng thái" });
            }

            order.Status = request.Status;
            if (request.Status == "Delivered")
            {
                order.ActualDeliveryDate = DateTime.Now;
            }

            if (!string.IsNullOrEmpty(request.Notes))
            {
                order.Notes = (order.Notes ?? "") + "\n" + request.Notes;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Cập nhật trạng thái thành công", order = new { id = order.Id, status = order.Status } });
        }

        // POST: api/ShipperApi/Orders/{id}/UpdateLocation
        [HttpPost("orders/{id}/location")]
        [Authorize(Roles = "Shipper", AuthenticationSchemes = ShipperAuthSchemes)]
        public async Task<IActionResult> UpdateLocation(int id, [FromBody] UpdateLocationRequest request)
        {
            var username = User.Identity?.Name;
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.Id == id && o.ShipperName == username);

            if (order == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            {
                return BadRequest(new { success = false, message = "Vui lòng cung cấp đầy đủ tọa độ (latitude, longitude)" });
            }

            // Lưu vị trí hiện tại của shipper vào Order
            order.ShipperLat = (decimal)request.Latitude.Value;
            order.ShipperLng = (decimal)request.Longitude.Value;
            order.ShipperLocationUpdatedAt = DateTime.Now;
            order.CurrentLocation = $"{request.Latitude},{request.Longitude}";

            await _context.SaveChangesAsync();

            // Broadcast location update via SignalR
            await _locationHub.Clients.Group($"order_{id}").SendAsync("LocationUpdated", new {
                orderId = id,
                latitude = request.Latitude.Value,
                longitude = request.Longitude.Value,
                timestamp = DateTime.Now
            });

            return Ok(new { 
                success = true, 
                message = "Cập nhật vị trí thành công",
                location = new {
                    latitude = request.Latitude.Value,
                    longitude = request.Longitude.Value,
                    timestamp = DateTime.Now
                }
            });
        }

        // GET: api/ShipperApi/Profile
        [HttpGet("profile")]
        [Authorize(Roles = "Shipper", AuthenticationSchemes = ShipperAuthSchemes)]
        public async Task<IActionResult> GetProfile()
        {
            var username = User.Identity?.Name;
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == username && u.Role == "Shipper");

            if (user == null)
            {
                return NotFound(new { success = false, message = "Không tìm thấy thông tin người dùng" });
            }

            var totalOrders = await _context.Orders
                .CountAsync(o => o.ShipperName == username);
            var deliveredOrders = await _context.Orders
                .CountAsync(o => o.ShipperName == username && o.Status == "Delivered");

            return Ok(new
            {
                success = true,
                user = new
                {
                    id = user.Id,
                    username = user.Username,
                    fullName = user.FullName,
                    email = user.Email,
                    phone = user.Phone,
                    address = user.Address,
                    avatar = user.Avatar,
                    stats = new
                    {
                        totalOrders = totalOrders,
                        deliveredOrders = deliveredOrders
                    }
                }
            });
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
            var key = Encoding.UTF8.GetBytes(jwtSettings.Key ?? "your-secret-key-here-must-be-at-least-32-characters-long");
            var issuer = jwtSettings.Issuer ?? "webgiaohang";
            var audience = jwtSettings.Audience ?? "webgiaohang";
            var expireMinutes = jwtSettings.ExpireMinutes > 0 ? jwtSettings.ExpireMinutes : 43200;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "Shipper")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(expireMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

        private static string ComputeHash(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }

    public class LoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class UpdateStatusRequest
    {
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }

    public class UpdateLocationRequest
    {
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
    }
}

