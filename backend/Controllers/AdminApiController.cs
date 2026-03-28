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
    public class AdminApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AdminApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AdminApi/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users
                .OrderByDescending(u => u.Id)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Email,
                    u.Phone,
                    u.Role,
                    u.IsApproved,
                    u.Avatar
                })
                .ToListAsync();

            return Ok(new { success = true, users });
        }

        // GET: api/AdminApi/shippers
        [HttpGet("shippers")]
        public async Task<IActionResult> GetShippers()
        {
            var shippers = await _context.Users
                .Where(u => u.Role == "Shipper" && u.IsApproved)
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    u.Id,
                    u.Username,
                    u.FullName,
                    u.Phone,
                    u.Email
                })
                .ToListAsync();

            return Ok(new { success = true, shippers });
        }

        // POST: api/AdminApi/users/{id}/approve
        [HttpPost("users/{id}/approve")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.IsApproved = true;
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Phê duyệt người dùng thành công!" });
        }

        // DELETE: api/AdminApi/users/{id}
        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // PUT: api/AdminApi/users/{id}/role
        [HttpPut("users/{id}/role")]
        public async Task<IActionResult> SetRole(int id, [FromBody] RoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Role = dto.Role;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // GET: api/AdminApi/dashboard
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            var totalOrders = await _context.Orders.CountAsync();
            var totalUsers = await _context.Users.CountAsync();
            var pendingOrders = await _context.Orders.CountAsync(o => o.Status == "Pending");
            var pendingUsers = await _context.Users.CountAsync(u => !u.IsApproved);

            return Ok(new
            {
                success = true,
                stats = new { totalOrders, totalUsers, pendingOrders, pendingUsers }
            });
        }
    }

    public class RoleDto { public string Role { get; set; } = ""; }
}
