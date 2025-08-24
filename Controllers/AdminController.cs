using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using webgiaohang.Data;
using System.Linq;
using webgiaohang.Models;
using System.Security.Cryptography;
using System.Text;

namespace webgiaohang.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Admin/Users
        public IActionResult Users()
        {
            var users = _context.Users.ToList();
            return View(users);
        }

        // POST: /Admin/DeleteUser/{id}
        [HttpPost]
        public IActionResult DeleteUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                _context.Users.Remove(user);
                _context.SaveChanges();
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult ApproveUser(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null && !user.IsApproved)
            {
                user.IsApproved = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        public IActionResult UpdateUserRole(int id, string role)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user != null && (role == "Customer" || role == "Staff" || role == "Shipper"))
            {
                user.Role = role;
                _context.SaveChanges();
            }
            return RedirectToAction("Users");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult SetUserRole(int id, string role)
        {
            var user = _context.Users.Find(id);
            if (user != null)
            {
                user.Role = role;
                user.IsApproved = true;
                _context.SaveChanges();
            }
            return RedirectToAction("Users");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public IActionResult RefreshUserAuth(int userId)
        {
            var user = _context.Users.Find(userId);
            if (user != null && user.IsApproved)
            {
                // Tạo thông báo cho user biết cần đăng nhập lại
                TempData["Message"] = $"Tài khoản {user.Username} đã được cập nhật. Vui lòng đăng nhập lại để áp dụng thay đổi.";
            }
            return RedirectToAction("Users");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ApproveAdmin()
        {
            var admin = _context.Users.FirstOrDefault(u => u.Username == "admin");
            if (admin != null)
            {
                admin.IsApproved = true;
                _context.SaveChanges();
                return Content("Tài khoản admin đã được phê duyệt. Bạn có thể đăng nhập lại.");
            }
            return Content("Không tìm thấy tài khoản admin.");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult PromoteAdmin()
        {
            var admin = _context.Users.FirstOrDefault(u => u.Username == "admin");
            if (admin != null)
            {
                admin.Role = "Admin";
                admin.IsApproved = true;
                _context.SaveChanges();
                return Content("Tài khoản admin đã được cập nhật quyền Admin và phê duyệt. Bạn có thể đăng nhập lại.");
            }
            return Content("Không tìm thấy tài khoản admin.");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult CreateSuperAdmin()
        {
            // Xóa admin cũ nếu tồn tại
            var oldAdmin = _context.Users.FirstOrDefault(u => u.Username == "admin");
            if (oldAdmin != null)
            {
                _context.Users.Remove(oldAdmin);
                _context.SaveChanges();
            }
            // Tạo superadmin mới
            if (!_context.Users.Any(u => u.Username == "superadmin"))
            {
                var superadmin = new User
                {
                    Username = "superadmin",
                    PasswordHash = ComputeHash("superadmin123"),
                    Role = "Admin",
                    IsApproved = true
                };
                _context.Users.Add(superadmin);
                _context.SaveChanges();
                return Content("Đã tạo tài khoản superadmin/superadmin123. Hãy đăng nhập lại.");
            }
            return Content("Tài khoản superadmin đã tồn tại.");
        }

        // GET: Admin/CreateUser
        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult CreateUser()
        {
            return View();
        }

        // POST: Admin/CreateUser
        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateUser(webgiaohang.Models.User user, string password)
        {
            if (string.IsNullOrEmpty(user.Username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(user.Role))
            {
                TempData["Message"] = "Vui lòng nhập đầy đủ thông tin.";
                return View(user);
            }
            if (_context.Users.Any(u => u.Username == user.Username))
            {
                TempData["Message"] = "Tên đăng nhập đã tồn tại.";
                return View(user);
            }
            user.PasswordHash = ComputeHash(password);
            user.IsApproved = true;
            user.CreatedDate = DateTime.Now;
            _context.Users.Add(user);
            _context.SaveChanges();
            TempData["Message"] = "Tạo tài khoản thành công!";
            return RedirectToAction("Users");
        }

        // GET: /Admin/Profile/{id}
        public IActionResult Profile(int id)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        // POST: /Admin/UpdateProfile/{id}
        [HttpPost]
        public IActionResult UpdateProfile(int id, string? fullName, string? email, string? phone, string? address, string? role, IFormFile? avatar)
        {
            var user = _context.Users.FirstOrDefault(u => u.Id == id);
            if (user == null) return NotFound();
            user.FullName = fullName;
            user.Email = email;
            user.Phone = phone;
            user.Address = address;
            if (!string.IsNullOrEmpty(role)) user.Role = role;
            // Xử lý upload avatar
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
            return RedirectToAction("Profile", new { id });
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