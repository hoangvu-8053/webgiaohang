using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/NotificationApi
        [HttpGet]
        public async Task<IActionResult> GetNotifications()
        {
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            var notifications = await _context.Notifications
                .Where(n => n.RecipientUsername == username || 
                            (n.RecipientRole != null && n.RecipientRole.Contains(role!)) ||
                            (n.RecipientUsername == null && n.RecipientRole == null))
                .OrderByDescending(n => n.CreatedAt)
                .Take(50)
                .ToListAsync();

            return Ok(new { success = true, notifications });
        }

        // POST: api/NotificationApi/{id}/read
        [HttpPost("{id}/read")]
        public async Task<IActionResult> MarkRead(int id)
        {
            var notif = await _context.Notifications.FindAsync(id);
            if (notif == null) return NotFound();

            notif.IsRead = true;
            notif.ReadAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // POST: api/NotificationApi/read-all
        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead()
        {
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            var notifs = await _context.Notifications
                .Where(n => !n.IsRead && (n.RecipientUsername == username || (n.RecipientRole != null && n.RecipientRole.Contains(role!))))
                .ToListAsync();

            foreach (var n in notifs) { n.IsRead = true; n.ReadAt = DateTime.Now; }
            await _context.SaveChangesAsync();

            return Ok(new { success = true });
        }

        // GET: api/NotificationApi/unread-count
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var username = User.Identity?.Name;
            var role = User.Claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Role)?.Value;

            var count = await _context.Notifications
                .CountAsync(n => !n.IsRead && (n.RecipientUsername == username || (n.RecipientRole != null && n.RecipientRole.Contains(role!))));

            return Ok(new { success = true, count });
        }
    }
}
