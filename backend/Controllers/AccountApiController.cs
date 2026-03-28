using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using webgiaohang.Data;
using webgiaohang.Models;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public AccountApiController(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        // POST: api/AccountApi/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
            if (user == null || user.PasswordHash != ComputeHash(dto.Password))
                return Unauthorized(new { success = false, message = "Sai tên đăng nhập hoặc mật khẩu." });

            if (!user.IsApproved)
                return Unauthorized(new { success = false, message = "Tài khoản chưa được phê duyệt. Vui lòng liên hệ admin." });

            // Tạo JWT token
            var token = GenerateJwtToken(user);

            // Cũng set cookie session
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(new ClaimsIdentity(claims, "Cookies")));

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
                    role = user.Role,
                    avatar = user.Avatar,
                    isApproved = user.IsApproved
                }
            });
        }

        // POST: api/AccountApi/register
        [HttpPost("register")]
        [AllowAnonymous]
        public IActionResult Register([FromBody] RegisterDto dto)
        {
            if (dto.Role != "Sender" && dto.Role != "Receiver")
                return BadRequest(new { success = false, message = "Chỉ cho phép đăng ký vai trò Người gửi hoặc Người nhận." });

            if (_context.Users.Any(u => u.Username == dto.Username))
                return BadRequest(new { success = false, message = "Tên đăng nhập đã tồn tại." });

            if (!string.IsNullOrEmpty(dto.Email) && _context.Users.Any(u => u.Email == dto.Email))
                return BadRequest(new { success = false, message = "Email đã được sử dụng." });

            var user = new User
            {
                Username = dto.Username,
                PasswordHash = ComputeHash(dto.Password),
                Role = dto.Role,
                IsApproved = false,
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone,
                Address = dto.Address
            };
            _context.Users.Add(user);
            _context.SaveChanges();

            return Ok(new { success = true, message = "Đăng ký thành công. Vui lòng chờ admin phê duyệt." });
        }

        // POST: api/AccountApi/logout
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return Ok(new { success = true, message = "Đăng xuất thành công." });
        }

        // GET: api/AccountApi/me
        [HttpGet("me")]
        [Authorize]
        public IActionResult Me()
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return Unauthorized(new { success = false, message = "Không xác thực được." });

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
                    role = user.Role,
                    avatar = user.Avatar,
                    isApproved = user.IsApproved,
                    bankQRCode = user.BankQRCode,
                    licensePlate = user.LicensePlate,
                    vehicleType = user.VehicleType
                }
            });
        }

        // PUT: api/AccountApi/profile
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromForm] UpdateProfileDto dto)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản." });

            user.FullName = dto.FullName;
            user.Email = dto.Email;
            user.Phone = dto.Phone;
            user.Address = dto.Address;

            if (dto.Avatar != null && dto.Avatar.Length > 0)
            {
                var fileName = $"avatar_{user.Id}_{DateTime.Now.Ticks}{Path.GetExtension(dto.Avatar.FileName)}";
                var path = Path.Combine("wwwroot", "avatars", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await dto.Avatar.CopyToAsync(stream);
                user.Avatar = "/avatars/" + fileName;
            }

            if (User.IsInRole("Shipper") && dto.BankQRCode != null && dto.BankQRCode.Length > 0)
            {
                var allowedExts = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var ext = Path.GetExtension(dto.BankQRCode.FileName).ToLowerInvariant();
                if (!allowedExts.Contains(ext))
                    return BadRequest(new { success = false, message = "Định dạng file không hợp lệ." });

                var fileName = $"bankqr_{user.Id}_{DateTime.Now.Ticks}{ext}";
                var path = Path.Combine("wwwroot", "shipper-qrcodes", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using var stream = new FileStream(path, FileMode.Create);
                await dto.BankQRCode.CopyToAsync(stream);
                user.BankQRCode = "/shipper-qrcodes/" + fileName;
            }

            _context.SaveChanges();
            return Ok(new { success = true, message = "Cập nhật hồ sơ thành công!" });
        }

        // POST: api/AccountApi/change-password
        [HttpPost("change-password")]
        [Authorize]
        public IActionResult ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound(new { success = false });

            if (user.PasswordHash != ComputeHash(dto.OldPassword))
                return BadRequest(new { success = false, message = "Mật khẩu cũ không đúng." });

            user.PasswordHash = ComputeHash(dto.NewPassword);
            _context.SaveChanges();
            return Ok(new { success = true, message = "Đổi mật khẩu thành công!" });
        }

        // POST: api/AccountApi/forgot-password
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public IActionResult ForgotPassword([FromBody] ForgotPasswordDto dto)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user == null)
                return BadRequest(new { success = false, message = "Email không tồn tại." });

            // Trong thực tế cần gửi email, tạm thời trả về code để test
            var code = new Random().Next(100000, 999999).ToString();
            // Lưu vào HttpContext.Session hoặc cache
            HttpContext.Session.SetString($"reset_{dto.Email}", code);
            return Ok(new { success = true, message = "Mã xác nhận đã được gửi về email.", debugCode = code });
        }

        // POST: api/AccountApi/reset-password
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public IActionResult ResetPassword([FromBody] ResetPasswordDto dto)
        {
            var savedCode = HttpContext.Session.GetString($"reset_{dto.Email}");
            if (savedCode == null || savedCode != dto.Code)
                return BadRequest(new { success = false, message = "Mã xác nhận không đúng hoặc đã hết hạn." });

            var user = _context.Users.FirstOrDefault(u => u.Email == dto.Email);
            if (user == null) return NotFound(new { success = false, message = "Không tìm thấy tài khoản." });

            user.PasswordHash = ComputeHash(dto.NewPassword);
            _context.SaveChanges();
            HttpContext.Session.Remove($"reset_{dto.Email}");
            return Ok(new { success = true, message = "Đặt lại mật khẩu thành công!" });
        }

        private string GenerateJwtToken(User user)
        {
            var jwt = _configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key));
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };
            var token = new JwtSecurityToken(
                issuer: jwt.Issuer,
                audience: jwt.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(jwt.ExpireMinutes > 0 ? jwt.ExpireMinutes : 43200),
                signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static string ComputeHash(string password)
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(password)));
        }
    }

    // DTOs
    public class LoginDto { public string Username { get; set; } = ""; public string Password { get; set; } = ""; }
    public class RegisterDto { public string Username { get; set; } = ""; public string Password { get; set; } = ""; public string Role { get; set; } = ""; public string? FullName { get; set; } public string? Email { get; set; } public string? Phone { get; set; } public string? Address { get; set; } }
    public class UpdateProfileDto { public string? FullName { get; set; } public string? Email { get; set; } public string? Phone { get; set; } public string? Address { get; set; } public IFormFile? Avatar { get; set; } public IFormFile? BankQRCode { get; set; } }
    public class ChangePasswordDto { public string OldPassword { get; set; } = ""; public string NewPassword { get; set; } = ""; }
    public class ForgotPasswordDto { public string Email { get; set; } = ""; }
    public class ResetPasswordDto { public string Email { get; set; } = ""; public string Code { get; set; } = ""; public string NewPassword { get; set; } = ""; }
}
