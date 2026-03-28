using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using webgiaohang.Data;
using webgiaohang.Models;
using webgiaohang.Hubs;

namespace webgiaohang.Services
{
    public interface INotificationService
    {
        Task CreateNotificationAsync(string title, string message, string? recipientUsername, string? recipientRole, string type, string? relatedEntityType, int? relatedEntityId);
        Task<int> GetUnreadCountAsync(string username);
        Task MarkAsReadAsync(int notificationId, string username);
        Task MarkAllAsReadAsync(string username);
    }

    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task CreateNotificationAsync(string title, string message, string? recipientUsername, string? recipientRole, string type, string? relatedEntityType, int? relatedEntityId)
        {
            var notification = new Notification
            {
                Title = title,
                Message = message,
                RecipientUsername = recipientUsername,
                RecipientRole = recipientRole,
                Type = type,
                RelatedEntityType = relatedEntityType,
                RelatedEntityId = relatedEntityId,
                CreatedAt = DateTime.Now
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Gửi SignalR notification real-time
            try
            {
                if (!string.IsNullOrEmpty(recipientUsername))
                {
                    // Gửi cho user cụ thể
                    await _hubContext.Clients.Group($"user_{recipientUsername}").SendAsync("ReceiveNotification", new
                    {
                        id = notification.Id,
                        title = notification.Title,
                        message = notification.Message,
                        type = notification.Type,
                        relatedEntityType = notification.RelatedEntityType,
                        relatedEntityId = notification.RelatedEntityId,
                        createdAt = notification.CreatedAt
                    });
                }
                else if (!string.IsNullOrEmpty(recipientRole))
                {
                    // Xử lý nhiều role được phân tách bởi dấu phẩy (ví dụ: "Admin,Staff")
                    var roles = recipientRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    var users = await _context.Users
                        .Where(u => roles.Contains(u.Role) && u.IsApproved)
                        .Select(u => u.Username)
                        .ToListAsync();

                    foreach (var username in users)
                    {
                        await _hubContext.Clients.Group($"user_{username}").SendAsync("ReceiveNotification", new
                        {
                            id = notification.Id,
                            title = notification.Title,
                            message = notification.Message,
                            type = notification.Type,
                            relatedEntityType = notification.RelatedEntityType,
                            relatedEntityId = notification.RelatedEntityId,
                            createdAt = notification.CreatedAt
                        });
                    }
                }
                else
                {
                    // Gửi cho tất cả user
                    await _hubContext.Clients.All.SendAsync("ReceiveNotification", new
                    {
                        id = notification.Id,
                        title = notification.Title,
                        message = notification.Message,
                        type = notification.Type,
                        relatedEntityType = notification.RelatedEntityType,
                        relatedEntityId = notification.RelatedEntityId,
                        createdAt = notification.CreatedAt
                    });
                }
            }
            catch (Exception ex)
            {
                // Log lỗi nhưng không chặn việc tạo notification
                System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi SignalR notification: {ex.Message}");
            }
        }

        public async Task<int> GetUnreadCountAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return 0;

            var query = _context.Notifications.Where(n => !n.IsRead);

            // Thông báo gửi cho user cụ thể
            var userNotifications = query.Where(n => n.RecipientUsername == username);

            // Thông báo gửi cho role của user (xử lý nhiều role được phân tách bởi dấu phẩy)
            var roleNotifications = query.Where(n => 
                n.RecipientUsername == null && 
                !string.IsNullOrEmpty(n.RecipientRole) &&
                (n.RecipientRole == user.Role || 
                 n.RecipientRole.StartsWith(user.Role + ",") || 
                 n.RecipientRole.EndsWith("," + user.Role) ||
                 n.RecipientRole.Contains("," + user.Role + ","))
            );

            // Thông báo gửi cho tất cả
            var allNotifications = query.Where(n => n.RecipientUsername == null && n.RecipientRole == null);

            var count = await userNotifications
                .Union(roleNotifications)
                .Union(allNotifications)
                .Distinct()
                .CountAsync();

            return count;
        }

        public async Task MarkAsReadAsync(int notificationId, string username)
        {
            var notification = await _context.Notifications.FindAsync(notificationId);
            if (notification != null && (notification.RecipientUsername == username || 
                notification.RecipientRole == null || notification.RecipientUsername == null))
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllAsReadAsync(string username)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
            if (user == null) return;

            var notifications = await _context.Notifications
                .Where(n => !n.IsRead && (
                    n.RecipientUsername == username ||
                    (n.RecipientUsername == null && !string.IsNullOrEmpty(n.RecipientRole) && 
                     (n.RecipientRole == user.Role || 
                      n.RecipientRole.StartsWith(user.Role + ",") || 
                      n.RecipientRole.EndsWith("," + user.Role) ||
                      n.RecipientRole.Contains("," + user.Role + ","))) ||
                    (n.RecipientUsername == null && n.RecipientRole == null)
                ))
                .ToListAsync();

            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
        }
    }
}

