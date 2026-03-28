using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;

namespace webgiaohang.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ChatApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/ChatApi/conversations
        [HttpGet("conversations")]
        public async Task<IActionResult> GetConversations()
        {
            var username = User.Identity?.Name;
            var sent = await _context.Messages.Where(m => m.SenderUsername == username).Select(m => m.ReceiverUsername).Distinct().ToListAsync();
            var received = await _context.Messages.Where(m => m.ReceiverUsername == username).Select(m => m.SenderUsername).Distinct().ToListAsync();

            var others = sent.Union(received).ToList();
            var conversations = new List<object>();

            foreach (var other in others)
            {
                var last = await _context.Messages
                    .Where(m => (m.SenderUsername == username && m.ReceiverUsername == other) || (m.SenderUsername == other && m.ReceiverUsername == username))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                conversations.Add(new { username = other, lastMessage = last });
            }

            return Ok(new { success = true, conversations });
        }

        // GET: api/ChatApi/messages
        [HttpGet("messages")]
        public async Task<IActionResult> GetMessages(string otherUsername, int? orderId)
        {
            var username = User.Identity?.Name;
            var query = _context.Messages.Where(m => (m.SenderUsername == username && m.ReceiverUsername == otherUsername) || (m.SenderUsername == otherUsername && m.ReceiverUsername == username));

            if (orderId.HasValue) query = query.Where(m => m.OrderId == orderId);

            var messages = await query.OrderBy(m => m.SentAt).ToListAsync();

            // Mark as read
            var unread = messages.Where(m => m.ReceiverUsername == username && !m.IsRead).ToList();
            foreach (var m in unread) { m.IsRead = true; m.ReadAt = DateTime.Now; }
            await _context.SaveChangesAsync();

            return Ok(new { success = true, messages });
        }

        // POST: api/ChatApi/send
        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] MessageDto dto)
        {
            var message = new Message
            {
                SenderUsername = User.Identity?.Name!,
                ReceiverUsername = dto.ReceiverUsername,
                Content = dto.Content,
                SentAt = DateTime.Now,
                OrderId = dto.OrderId,
                MessageType = "Text"
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message });
        }

        // GET: api/ChatApi/users
        [HttpGet("users")]
        public async Task<IActionResult> GetUsers()
        {
            var users = await _context.Users.Where(u => u.IsApproved).Select(u => u.Username).ToListAsync();
            return Ok(new { success = true, users });
        }
    }

    public class MessageDto { public string ReceiverUsername { get; set; } = ""; public string Content { get; set; } = ""; public int? OrderId { get; set; } }
}
