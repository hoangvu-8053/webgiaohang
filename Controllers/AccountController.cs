using Microsoft.AspNetCore.Mvc;
using webgiaohang.Models;
using webgiaohang.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace webgiaohang.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string password, string role, string fullName, string email, string phone, string address)
        {
           
            if (role != "Sender" && role != "Receiver")
            {
                ModelState.AddModelError("", "Chỉ cho phép đăng ký vai trò Người gửi hàng hoặc Người nhận hàng.");
                return View();
            }
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại.");
                return View();
            }
           
            if (_context.Users.Any(u => u.Email == email))
            {
                ModelState.AddModelError("", "Email đã được sử dụng cho tài khoản khác.");
                return View();
            }
            var user = new User
            {
                Username = username,
                PasswordHash = ComputeHash(password),
                Role = role,
                IsApproved = false, 
                FullName = fullName,
                Email = email,
                Phone = phone,
                Address = address
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            ViewBag.Message = "Đăng ký thành công, vui lòng chờ admin phê duyệt tài khoản.";
            return View();
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null || user.PasswordHash != ComputeHash(password))
            {
                ModelState.AddModelError("", "Sai tên đăng nhập hoặc mật khẩu.");
                return View();
            }
            if (!user.IsApproved)
            {
                ModelState.AddModelError("", "Tài khoản chưa được phê duyệt. Vui lòng liên hệ admin.");
                return View();
            }
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username ?? ""),
                new Claim(ClaimTypes.Role, user.Role ?? "Customer")
            };
            var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
            await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));
            return RedirectToAction("Index", "Orders");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> RefreshAuth()
        {
            var username = User.Identity!.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            
            if (user != null && user.IsApproved)
            {
               
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username ?? ""),
                    new Claim(ClaimTypes.Role, user.Role ?? "Customer")
                };
                var claimsIdentity = new ClaimsIdentity(claims, "Cookies");
                await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity));
                
                return RedirectToAction("Index", "Orders");
            }
            
            return RedirectToAction("Login");
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToAction("Login");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult CreateUser(string username, string password, string role)
        {
            if (_context.Users.Any(u => u.Username == username))
            {
                ModelState.AddModelError("", "Tên đăng nhập đã tồn tại.");
                return View();
            }
            var user = new User
            {
                Username = username,
                PasswordHash = ComputeHash(password),
                Role = role
            };
            _context.Users.Add(user);
            _context.SaveChanges();
            ViewBag.Message = "Tạo tài khoản thành công!";
            return View();
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

      
        [Authorize]
        public IActionResult Profile()
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound();
            return View(user);
        }

       
        [Authorize]
        [HttpPost]
        public IActionResult Profile(string? fullName, string? email, string? phone, string? address, IFormFile? avatar)
        {
            var username = User.Identity?.Name;
            var user = _context.Users.FirstOrDefault(u => u.Username == username);
            if (user == null) return NotFound();
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.Address = address;
            
            if (avatar != null && avatar.Length > 0)
            {
                var fileName = $"avatar_{user.Id}_{DateTime.Now.Ticks}{System.IO.Path.GetExtension(avatar.FileName)}";
                var path = Path.Combine("wwwroot", "avatars", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                using (var stream = new FileStream(path, FileMode.Create))
                {
                    avatar.CopyTo(stream);
                }
                user.Avatar = "/avatars/" + fileName;
            }
            _context.SaveChanges();
            TempData["Message"] = "Cập nhật hồ sơ thành công!";
            return RedirectToAction("Profile");
        }

       
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ForgotPassword(string email)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "Email không tồn tại trong hệ thống.");
                return View();
            }

            var code = new Random().Next(100000, 999999).ToString();
            TempData["ResetCode"] = code;
            TempData["ResetEmail"] = email;
            ViewBag.Code = code;
            return View("EnterResetCode");
        }

        
        [HttpGet]
        public IActionResult EnterResetCode()
        {
            return View();
        }

        // XỬ LÝ MÃ XÁC NHẬN
        [HttpPost]
        public IActionResult EnterResetCode(string code)
        {
            var expectedCode = TempData["ResetCode"] as string;
            var email = TempData["ResetEmail"] as string;
            if (expectedCode == null || email == null)
            {
                ModelState.AddModelError("", "Phiên làm việc đã hết hạn. Vui lòng thử lại.");
                return RedirectToAction("ForgotPassword");
            }
            if (code != expectedCode)
            {
                ModelState.AddModelError("", "Mã xác nhận không đúng.");
                TempData["ResetCode"] = expectedCode;
                TempData["ResetEmail"] = email;
                return View();
            }
            TempData["AllowReset"] = true;
            TempData["ResetEmail"] = email;
            return RedirectToAction("ResetPassword");
        }

        // HIỂN THỊ FORM ĐẶT LẠI MẬT KHẨU
        [HttpGet]
        public IActionResult ResetPassword()
        {
            if (TempData["AllowReset"] == null || TempData["ResetEmail"] == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            TempData.Keep("AllowReset");
            TempData.Keep("ResetEmail");
            return View();
        }

        // XỬ LÝ ĐẶT LẠI MẬT KHẨU
        [HttpPost]
        public IActionResult ResetPassword(string password)
        {
            var email = TempData["ResetEmail"] as string;
            if (email == null)
            {
                return RedirectToAction("ForgotPassword");
            }
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null)
            {
                ModelState.AddModelError("", "Không tìm thấy tài khoản.");
                return View();
            }
            user.PasswordHash = ComputeHash(password);
            _context.SaveChanges();
            ViewBag.Message = "Đặt lại mật khẩu thành công. Bạn có thể đăng nhập với mật khẩu mới.";
            return View();
        }

        private string ComputeHash(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
} 