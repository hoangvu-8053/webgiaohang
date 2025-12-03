using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using webgiaohang.Data;
using webgiaohang.Models;
using System.Security.Claims;
using webgiaohang.Services;
using System.Linq;

namespace webgiaohang.Controllers
{
    [Authorize]
    public class ChatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public ChatController(ApplicationDbContext context, INotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        // Kiểm tra xem có Admin/Staff nào đang online không
        private bool IsSupportOnline()
        {
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);
            var onlineAdmins = _context.Users
                .Where(u => (u.Role == "Admin" || u.Role == "Staff") && 
                           u.IsActive && 
                           u.IsApproved &&
                           u.LastLoginDate.HasValue &&
                           u.LastLoginDate.Value >= fiveMinutesAgo)
                .Any();
            return onlineAdmins;
        }

        // Lấy tất cả Admin/Staff online
        private List<User> GetOnlineSupport()
        {
            var fiveMinutesAgo = DateTime.Now.AddMinutes(-5);
            return _context.Users
                .Where(u => (u.Role == "Admin" || u.Role == "Staff") && 
                           u.IsActive && 
                           u.IsApproved &&
                           u.LastLoginDate.HasValue &&
                           u.LastLoginDate.Value >= fiveMinutesAgo)
                .ToList();
        }

        // GET: Chat/Select - Chọn người để chat
        public IActionResult Select(int? orderId)
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            var currentUser = _context.Users.FirstOrDefault(u => u.Username == currentUsername);
            var availableUsers = new List<object>();

            if (orderId.HasValue)
            {
                // Lấy danh sách người liên quan đến đơn hàng
                var order = _context.Orders.FirstOrDefault(o => o.Id == orderId.Value);
                if (order != null)
                {
                    // Kiểm tra quyền truy cập
                    var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
                    var isSender = order.SenderName == currentUsername || order.CreatedBy == currentUsername;
                    var isReceiver = order.ReceiverName == currentUsername;
                    var isShipper = order.ShipperName == currentUsername;

                    if (!isAdminOrStaff && !isSender && !isReceiver && !isShipper)
                    {
                        return Forbid("Bạn không có quyền xem đơn hàng này");
                    }

                    // Thêm người gửi
                    if (!string.IsNullOrEmpty(order.SenderName) && order.SenderName != currentUsername)
                    {
                        var senderUser = _context.Users.FirstOrDefault(u => u.Username == order.SenderName);
                        availableUsers.Add(new
                        {
                            Username = order.SenderName,
                            User = senderUser,
                            Role = "Người gửi",
                            RoleCode = "Sender",
                            OrderId = orderId.Value,
                            Order = order
                        });
                    }

                    // Thêm người nhận
                    if (!string.IsNullOrEmpty(order.ReceiverName) && order.ReceiverName != currentUsername)
                    {
                        var receiverUser = _context.Users.FirstOrDefault(u => u.Username == order.ReceiverName);
                        availableUsers.Add(new
                        {
                            Username = order.ReceiverName,
                            User = receiverUser,
                            Role = "Người nhận",
                            RoleCode = "Receiver",
                            OrderId = orderId.Value,
                            Order = order
                        });
                    }

                    // Thêm shipper
                    if (!string.IsNullOrEmpty(order.ShipperName) && order.ShipperName != currentUsername)
                    {
                        var shipperUser = _context.Users.FirstOrDefault(u => u.Username == order.ShipperName);
                        availableUsers.Add(new
                        {
                            Username = order.ShipperName,
                            User = shipperUser,
                            Role = "Shipper",
                            RoleCode = "Shipper",
                            OrderId = orderId.Value,
                            Order = order
                        });
                    }

                    // Nếu là Admin/Staff, thêm tất cả người liên quan
                    if (isAdminOrStaff)
                    {
                        ViewBag.Order = order;
                    }
                }
            }
            else
            {
                // Lấy danh sách tất cả người dùng có thể chat (theo role)
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    // Admin/Staff có thể chat với tất cả
                    var allUsers = _context.Users
                        .Where(u => u.Username != currentUsername && u.IsApproved)
                        .ToList();
                    
                    foreach (var user in allUsers)
                    {
                        availableUsers.Add(new
                        {
                            Username = user.Username,
                            User = user,
                            Role = GetRoleDisplayName(user.Role),
                            RoleCode = user.Role
                        });
                    }
                }
                else if (User.IsInRole("Sender"))
                {
                    // Thêm option chat với Hỗ trợ (Admin/Staff)
                    var isSupportOnline = IsSupportOnline();
                    availableUsers.Add(new
                    {
                        Username = "SUPPORT",
                        User = (User?)null,
                        Role = "Hỗ trợ",
                        RoleCode = "Support",
                        IsOnline = isSupportOnline
                    });

                    // Sender có thể chat với Receiver và Shipper từ các đơn hàng của mình
                    var myOrders = _context.Orders
                        .Where(o => o.CreatedBy == currentUsername || o.SenderName == currentUsername)
                        .ToList();

                    var uniqueUsers = new HashSet<string>();
                    foreach (var order in myOrders)
                    {
                        if (!string.IsNullOrEmpty(order.ReceiverName) && order.ReceiverName != currentUsername)
                        {
                            uniqueUsers.Add(order.ReceiverName);
                        }
                        if (!string.IsNullOrEmpty(order.ShipperName) && order.ShipperName != currentUsername)
                        {
                            uniqueUsers.Add(order.ShipperName);
                        }
                    }

                    foreach (var username in uniqueUsers)
                    {
                        var user = _context.Users.FirstOrDefault(u => u.Username == username);
                        if (user != null)
                        {
                            availableUsers.Add(new
                            {
                                Username = username,
                                User = user,
                                Role = GetRoleDisplayName(user.Role),
                                RoleCode = user.Role
                            });
                        }
                    }
                }
                else if (User.IsInRole("Receiver"))
                {
                    // Thêm option chat với Hỗ trợ (Admin/Staff)
                    var isSupportOnline = IsSupportOnline();
                    availableUsers.Add(new
                    {
                        Username = "SUPPORT",
                        User = (User?)null,
                        Role = "Hỗ trợ",
                        RoleCode = "Support",
                        IsOnline = isSupportOnline
                    });

                    // Receiver có thể chat với Sender và Shipper từ các đơn hàng của mình
                    var myOrders = _context.Orders
                        .Where(o => o.ReceiverName == currentUsername)
                        .ToList();

                    var uniqueUsers = new HashSet<string>();
                    foreach (var order in myOrders)
                    {
                        if (!string.IsNullOrEmpty(order.SenderName) && order.SenderName != currentUsername)
                        {
                            uniqueUsers.Add(order.SenderName);
                        }
                        if (!string.IsNullOrEmpty(order.ShipperName) && order.ShipperName != currentUsername)
                        {
                            uniqueUsers.Add(order.ShipperName);
                        }
                    }

                    foreach (var username in uniqueUsers)
                    {
                        var user = _context.Users.FirstOrDefault(u => u.Username == username);
                        if (user != null)
                        {
                            availableUsers.Add(new
                            {
                                Username = username,
                                User = user,
                                Role = GetRoleDisplayName(user.Role),
                                RoleCode = user.Role
                            });
                        }
                    }
                }
                else if (User.IsInRole("Shipper"))
                {
                    // Thêm option chat với Hỗ trợ (Admin/Staff)
                    var isSupportOnline = IsSupportOnline();
                    availableUsers.Add(new
                    {
                        Username = "SUPPORT",
                        User = (User?)null,
                        Role = "Hỗ trợ",
                        RoleCode = "Support",
                        IsOnline = isSupportOnline
                    });

                    // Shipper có thể chat với Sender và Receiver từ các đơn hàng của mình
                    var myOrders = _context.Orders
                        .Where(o => o.ShipperName == currentUsername)
                        .ToList();

                    var uniqueUsers = new HashSet<string>();
                    foreach (var order in myOrders)
                    {
                        if (!string.IsNullOrEmpty(order.SenderName) && order.SenderName != currentUsername)
                        {
                            uniqueUsers.Add(order.SenderName);
                        }
                        if (!string.IsNullOrEmpty(order.ReceiverName) && order.ReceiverName != currentUsername)
                        {
                            uniqueUsers.Add(order.ReceiverName);
                        }
                    }

                    foreach (var username in uniqueUsers)
                    {
                        var user = _context.Users.FirstOrDefault(u => u.Username == username);
                        if (user != null)
                        {
                            availableUsers.Add(new
                            {
                                Username = username,
                                User = user,
                                Role = GetRoleDisplayName(user.Role),
                                RoleCode = user.Role
                            });
                        }
                    }
                }
            }

            ViewBag.AvailableUsers = availableUsers;
            ViewBag.CurrentUsername = currentUsername;
            ViewBag.CurrentUser = currentUser;
            ViewBag.OrderId = orderId;

            return View();
        }

        // Helper method để lấy tên hiển thị của role
        private string GetRoleDisplayName(string? role)
        {
            return role switch
            {
                "Admin" => "Quản trị viên",
                "Staff" => "Nhân viên",
                "Shipper" => "Shipper",
                "Sender" => "Người gửi",
                "Receiver" => "Người nhận",
                "Customer" => "Khách hàng",
                _ => role ?? "Người dùng"
            };
        }

        // GET: Chat - Danh sách các cuộc trò chuyện
        public IActionResult Index()
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách người đã chat với user hiện tại
            List<Message> allMessages;
            
            if (User.IsInRole("Admin") || User.IsInRole("Staff"))
            {
                // Admin/Staff thấy tất cả tin nhắn gửi đến SUPPORT
                allMessages = _context.Messages
                    .Where(m => m.ReceiverUsername == "SUPPORT" || 
                               (m.SenderUsername == currentUsername && m.ReceiverUsername != "SUPPORT") ||
                               (m.ReceiverUsername == currentUsername && m.SenderUsername != "SUPPORT"))
                    .ToList();
            }
            else
            {
                // Khách hàng thấy tin nhắn của mình với SUPPORT và các chat khác
                allMessages = _context.Messages
                    .Where(m => (m.SenderUsername == currentUsername || m.ReceiverUsername == currentUsername) &&
                               (m.ReceiverUsername == "SUPPORT" || m.SenderUsername == "SUPPORT" || 
                                (m.ReceiverUsername != "SUPPORT" && m.SenderUsername != "SUPPORT")))
                    .ToList();
            }

            var conversationGroups = allMessages
                .GroupBy(m => {
                    if (m.SenderUsername == currentUsername)
                        return m.ReceiverUsername;
                    else if (m.ReceiverUsername == currentUsername)
                        return m.SenderUsername;
                    else if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                    {
                        // Admin/Staff thấy tin nhắn từ khách hàng gửi đến SUPPORT
                        if (m.ReceiverUsername == "SUPPORT")
                            return "SUPPORT";
                    }
                    return m.SenderUsername == currentUsername ? m.ReceiverUsername : m.SenderUsername;
                })
                .ToList();

            var conversationList = new List<object>();
            foreach (var group in conversationGroups)
            {
                var otherUsername = group.Key;
                var groupMessages = group.ToList();
                var lastMessage = groupMessages.OrderByDescending(m => m.SentAt).FirstOrDefault();
                var unreadCount = groupMessages.Count(m => m.ReceiverUsername == currentUsername && !m.IsRead);
                
                var otherUser = _context.Users.FirstOrDefault(u => u.Username == otherUsername);
                conversationList.Add(new
                {
                    OtherUsername = otherUsername,
                    OtherUser = otherUser,
                    OtherUserRole = otherUser?.Role ?? "Customer",
                    OtherUserRoleDisplay = GetRoleDisplayName(otherUser?.Role),
                    LastMessage = lastMessage,
                    UnreadCount = unreadCount,
                    OrderId = lastMessage?.OrderId,
                    Order = lastMessage?.OrderId.HasValue == true ? _context.Orders.FirstOrDefault(o => o.Id == lastMessage.OrderId.Value) : null
                });
            }

            conversationList = conversationList
                .OrderByDescending(c => {
                    var prop = c.GetType().GetProperty("LastMessage");
                    var msg = prop?.GetValue(c) as Message;
                    return msg?.SentAt ?? DateTime.MinValue;
                })
                .ToList();

            ViewBag.Conversations = conversationList;
            ViewBag.CurrentUsername = currentUsername;
            ViewBag.CurrentUser = _context.Users.FirstOrDefault(u => u.Username == currentUsername);

            return View();
        }

        // GET: Chat/Chat?otherUsername=xxx&orderId=123
        public IActionResult Chat(string? otherUsername, int? orderId)
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return RedirectToAction("Login", "Account");
            }

            // Cập nhật LastLoginDate khi truy cập chat
            var currentUser = _context.Users.FirstOrDefault(u => u.Username == currentUsername);
            if (currentUser != null)
            {
                currentUser.LastLoginDate = DateTime.Now;
                _context.SaveChanges();
            }

            if (string.IsNullOrEmpty(otherUsername))
            {
                return RedirectToAction("Index");
            }

            // Xử lý chat với SUPPORT (Admin/Staff gộp chung)
            if (otherUsername == "SUPPORT")
            {
                // Nếu là Admin/Staff, hiển thị tất cả tin nhắn từ khách hàng và tin nhắn trả lời
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    // Lấy tất cả tin nhắn gửi đến SUPPORT và tin nhắn từ SUPPORT trả lời
                    var supportMessages = _context.Messages
                        .Where(m => m.ReceiverUsername == "SUPPORT" || m.SenderUsername == "SUPPORT")
                        .OrderBy(m => m.SentAt)
                        .ToList();

                    ViewBag.Messages = supportMessages;
                    ViewBag.CurrentUsername = currentUsername;
                    ViewBag.OtherUsername = "SUPPORT";
                    ViewBag.CurrentUser = currentUser;
                    ViewBag.OtherUser = null;
                    ViewBag.OrderId = orderId;
                    ViewBag.IsSupportChat = true;
                    ViewBag.IsSupportOnline = IsSupportOnline();

                    return View();
                }
                else
                {
                    // Nếu là khách hàng, hiển thị tin nhắn giữa họ và SUPPORT
                    var customerSupportMessages = _context.Messages
                        .Where(m => 
                            (m.SenderUsername == currentUsername && m.ReceiverUsername == "SUPPORT") ||
                            (m.SenderUsername == "SUPPORT" && m.ReceiverUsername == currentUsername))
                        .OrderBy(m => m.SentAt)
                        .ToList();

                    // Đánh dấu tin nhắn đã đọc
                    var unreadCustomerMessages = customerSupportMessages.Where(m => m.ReceiverUsername == currentUsername && !m.IsRead).ToList();
                    foreach (var msgItem in unreadCustomerMessages)
                    {
                        msgItem.IsRead = true;
                        msgItem.ReadAt = DateTime.Now;
                    }
                    _context.SaveChanges();

                    ViewBag.Messages = customerSupportMessages;
                    ViewBag.CurrentUsername = currentUsername;
                    ViewBag.OtherUsername = "SUPPORT";
                    ViewBag.CurrentUser = currentUser;
                    ViewBag.OtherUser = null;
                    ViewBag.OrderId = orderId;
                    ViewBag.IsSupportChat = true;
                    ViewBag.IsSupportOnline = IsSupportOnline();

                    return View();
                }
            }

            // Kiểm tra quyền truy cập cho chat thông thường
            var otherUser = _context.Users.FirstOrDefault(u => u.Username == otherUsername);
            
            if (otherUser == null)
            {
                return NotFound("Người dùng không tồn tại");
            }

            // Nếu có orderId hợp lệ (> 0), kiểm tra quyền truy cập order
            if (orderId.HasValue && orderId.Value > 0)
            {
                var order = _context.Orders.FirstOrDefault(o => o.Id == orderId.Value);
                if (order == null)
                {
                    return NotFound("Đơn hàng không tồn tại");
                }

                // Kiểm tra quyền: Admin, Staff, Sender, Receiver, Shipper của order này
                var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
                var isSender = order.SenderName == currentUsername || order.CreatedBy == currentUsername;
                var isReceiver = order.ReceiverName == currentUsername;
                var isShipper = order.ShipperName == currentUsername;

                if (!isAdminOrStaff && !isSender && !isReceiver && !isShipper)
                {
                    return Forbid("Bạn không có quyền truy cập cuộc trò chuyện này");
                }

                // Kiểm tra otherUsername có liên quan đến order không
                var isValidParticipant = 
                    order.SenderName == otherUsername || 
                    order.ReceiverName == otherUsername || 
                    order.ShipperName == otherUsername ||
                    isAdminOrStaff;

                if (!isValidParticipant && !User.IsInRole("Admin") && !User.IsInRole("Staff"))
                {
                    return Forbid("Người dùng này không liên quan đến đơn hàng");
                }

                ViewBag.Order = order;
            }

            // Lấy tin nhắn giữa 2 người
            var messages = _context.Messages
                .Where(m => 
                    ((m.SenderUsername == currentUsername && m.ReceiverUsername == otherUsername) ||
                     (m.SenderUsername == otherUsername && m.ReceiverUsername == currentUsername)) &&
                    (orderId == null || orderId.Value == 0 || m.OrderId == orderId))
                .OrderBy(m => m.SentAt)
                .ToList();

            // Đánh dấu tin nhắn đã đọc
            var unreadMessages = messages.Where(m => m.ReceiverUsername == currentUsername && !m.IsRead).ToList();
            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.Now;
            }
            _context.SaveChanges();

            ViewBag.Messages = messages;
            ViewBag.CurrentUsername = currentUsername;
            ViewBag.OtherUsername = otherUsername;
            ViewBag.CurrentUser = currentUser;
            ViewBag.OtherUser = otherUser;
            ViewBag.OrderId = orderId;

            return View();
        }

        // POST: Chat/SendMessage
        [HttpPost]
        public async Task<IActionResult> SendMessage(string receiverUsername, string content, int? orderId)
        {
            try
            {
                var currentUsername = User.Identity?.Name;
                if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(receiverUsername) || string.IsNullOrEmpty(content))
                {
                    return Json(new { success = false, message = "Thông tin không hợp lệ" });
                }

                // Validate và trim content
                content = content?.Trim() ?? string.Empty;
                if (string.IsNullOrEmpty(content))
                {
                    return Json(new { success = false, message = "Nội dung tin nhắn không được để trống" });
                }

                // Giới hạn độ dài content
                if (content.Length > 2000)
                {
                    content = content.Substring(0, 2000);
                }

                // Lấy thông tin user hiện tại
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.Username == currentUsername);
                
                // Cập nhật LastLoginDate (không chặn nếu lỗi)
                if (currentUser != null)
                {
                    try
                    {
                        currentUser.LastLoginDate = DateTime.Now;
                        await _context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi cập nhật LastLoginDate: {ex.Message}");
                    }
                }

                // Xử lý gửi tin nhắn đến SUPPORT
                // Chỉ cho phép Sender, Receiver, Shipper gửi tin nhắn đến SUPPORT
                // Admin/Staff không gửi tin nhắn ĐẾN SUPPORT mà trả lời TỪ SUPPORT
                if (receiverUsername == "SUPPORT")
                {
                    // Kiểm tra role: Admin/Staff không nên gửi tin nhắn ĐẾN SUPPORT
                    if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                    {
                        return Json(new { success = false, message = "Bạn không thể gửi tin nhắn đến SUPPORT. Vui lòng chọn khách hàng để trả lời." });
                    }

                    // Kiểm tra orderId nếu có (chỉ khi > 0)
                    // Khi chat với SUPPORT, không cần kiểm tra quyền order vì đây là chat hỗ trợ
                    int? supportOrderId = null;
                    if (orderId.HasValue && orderId.Value > 0)
                    {
                        var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId.Value);
                        if (orderExists)
                        {
                            supportOrderId = orderId.Value;
                        }
                    }

                    var supportMessage = new Message
                    {
                        SenderUsername = currentUsername,
                        ReceiverUsername = "SUPPORT",
                        Content = content,
                        SentAt = DateTime.Now,
                        OrderId = supportOrderId,
                        MessageType = "Text",
                        IsRead = false
                    };

                    _context.Messages.Add(supportMessage);
                    await _context.SaveChangesAsync();

                    // Tạo thông báo cho Admin/Staff khi có tin nhắn mới từ khách hàng (không chặn nếu lỗi)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                "Tin nhắn mới từ khách hàng",
                                $"{currentUser?.FullName ?? currentUsername} đã gửi tin nhắn: {content.Substring(0, Math.Min(50, content.Length))}...",
                                null,
                                "Admin,Staff",
                                "Info",
                                "Message",
                                supportMessage.Id
                            );
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo thông báo: {ex.Message}");
                        }
                    });

                    // Kiểm tra xem có Admin/Staff nào online không
                    var isSupportOnline = IsSupportOnline();
                    if (!isSupportOnline)
                    {
                        // Tự động gửi tin nhắn thông báo
                        var autoMessage = new Message
                        {
                            SenderUsername = "SUPPORT",
                            ReceiverUsername = currentUsername,
                            Content = "Chúng tôi sẽ trả lời bạn trong vòng sớm nhất. Cảm ơn bạn đã liên hệ!",
                            SentAt = DateTime.Now,
                            OrderId = supportOrderId,
                            MessageType = "System",
                            IsRead = false
                        };

                        _context.Messages.Add(autoMessage);
                        await _context.SaveChangesAsync();

                        return Json(new { 
                            success = true, 
                            messageId = supportMessage.Id,
                            sentAt = supportMessage.SentAt.ToString("dd/MM/yyyy HH:mm"),
                            autoMessage = new {
                                id = autoMessage.Id,
                                content = autoMessage.Content,
                                sentAt = autoMessage.SentAt.ToString("dd/MM/yyyy HH:mm")
                            }
                        });
                    }

                    return Json(new { 
                        success = true, 
                        messageId = supportMessage.Id,
                        sentAt = supportMessage.SentAt.ToString("dd/MM/yyyy HH:mm")
                    });
                }

                // Xử lý Admin/Staff trả lời tin nhắn từ SUPPORT
                // Khi Admin/Staff trả lời, họ gửi tin nhắn đến khách hàng (receiverUsername là username của khách hàng)
                // Tin nhắn sẽ được gửi với SenderUsername = "SUPPORT" để khách hàng thấy đúng
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    // Kiểm tra xem đây có phải là trả lời tin nhắn từ khách hàng không
                    // (tức là có tin nhắn từ khách hàng gửi đến SUPPORT)
                    var originalMessage = await _context.Messages
                        .Where(m => m.ReceiverUsername == "SUPPORT" && m.SenderUsername == receiverUsername)
                        .OrderByDescending(m => m.SentAt)
                        .FirstOrDefaultAsync();

                    if (originalMessage != null)
                    {
                        // Kiểm tra orderId nếu có
                        int? replyOrderId = originalMessage.OrderId;
                        if (orderId.HasValue && orderId.Value > 0)
                        {
                            var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId.Value);
                            if (orderExists)
                            {
                                replyOrderId = orderId.Value;
                            }
                        }

                        // Gửi tin nhắn trả lời với SenderUsername = "SUPPORT" để khách hàng thấy như từ SUPPORT
                        var replyMessage = new Message
                        {
                            SenderUsername = "SUPPORT", // Dùng "SUPPORT" thay vì tên admin để khách hàng thấy đúng
                            ReceiverUsername = receiverUsername,
                            Content = content,
                            SentAt = DateTime.Now,
                            OrderId = replyOrderId,
                            MessageType = "Text",
                            IsRead = false
                        };

                        _context.Messages.Add(replyMessage);
                        
                        // Đánh dấu tin nhắn gốc đã được trả lời (tất cả tin nhắn chưa đọc từ khách hàng này)
                        var unreadMessages = await _context.Messages
                            .Where(m => m.ReceiverUsername == "SUPPORT" && 
                                       m.SenderUsername == receiverUsername && 
                                       !m.IsRead)
                            .ToListAsync();
                        
                        foreach (var msg in unreadMessages)
                        {
                            msg.IsRead = true;
                            msg.ReadAt = DateTime.Now;
                        }
                        
                        await _context.SaveChangesAsync();

                        // Tạo thông báo cho người nhận khi Admin/Staff trả lời (không chặn nếu lỗi)
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await _notificationService.CreateNotificationAsync(
                                    "Hỗ trợ đã trả lời",
                                    $"Hỗ trợ đã trả lời tin nhắn của bạn: {content.Substring(0, Math.Min(50, content.Length))}...",
                                    receiverUsername,
                                    null,
                                    "Info",
                                    "Message",
                                    replyMessage.Id
                                );
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo thông báo: {ex.Message}");
                            }
                        });

                        return Json(new { 
                            success = true, 
                            messageId = replyMessage.Id,
                            sentAt = replyMessage.SentAt.ToString("dd/MM/yyyy HH:mm")
                        });
                    }
                }

                // Kiểm tra quyền nếu có orderId
                int? messageOrderId = null;
                if (orderId.HasValue && orderId.Value > 0)
                {
                    var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
                    if (order != null)
                    {
                        var isAdminOrStaff = User.IsInRole("Admin") || User.IsInRole("Staff");
                        var isSender = order.SenderName == currentUsername || order.CreatedBy == currentUsername;
                        var isReceiver = order.ReceiverName == currentUsername;
                        var isShipper = order.ShipperName == currentUsername;

                        if (!isAdminOrStaff && !isSender && !isReceiver && !isShipper)
                        {
                            return Json(new { success = false, message = "Bạn không có quyền gửi tin nhắn trong đơn hàng này" });
                        }
                        messageOrderId = orderId.Value;
                    }
                }

                var message = new Message
                {
                    SenderUsername = currentUsername,
                    ReceiverUsername = receiverUsername,
                    Content = content,
                    SentAt = DateTime.Now,
                    OrderId = messageOrderId,
                    MessageType = "Text",
                    IsRead = false
                };

                _context.Messages.Add(message);
                await _context.SaveChangesAsync();

                // Tạo thông báo cho người nhận khi có tin nhắn mới (không chặn nếu lỗi)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        var senderDisplayName = currentUser?.FullName ?? currentUsername;
                        await _notificationService.CreateNotificationAsync(
                            "Tin nhắn mới",
                            $"{senderDisplayName} đã gửi tin nhắn cho bạn: {content.Substring(0, Math.Min(50, content.Length))}...",
                            receiverUsername,
                            null,
                            "Info",
                            "Message",
                            message.Id
                        );
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Lỗi khi tạo thông báo: {ex.Message}");
                    }
                });

                return Json(new { 
                    success = true, 
                    messageId = message.Id,
                    sentAt = message.SentAt.ToString("dd/MM/yyyy HH:mm")
                });
            }
            catch (Exception ex)
            {
                // Log chi tiết lỗi
                var errorMessage = ex.Message;
                if (ex.InnerException != null)
                {
                    errorMessage += " | Inner: " + ex.InnerException.Message;
                }
                System.Diagnostics.Debug.WriteLine($"Lỗi khi gửi tin nhắn: {errorMessage}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");

                return Json(new { 
                    success = false, 
                    message = "Có lỗi xảy ra khi gửi tin nhắn: " + errorMessage
                });
            }
        }

        // GET: Chat/GetMessages?otherUsername=xxx&orderId=123&lastMessageId=456
        [HttpGet]
        public IActionResult GetMessages(string? otherUsername, int? orderId, int? lastMessageId)
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername) || string.IsNullOrEmpty(otherUsername))
            {
                return Json(new { success = false, messages = new List<object>() });
            }

            // Xử lý đặc biệt cho chat với SUPPORT
            IQueryable<Message> query;
            if (otherUsername == "SUPPORT")
            {
                // Nếu là Admin/Staff, lấy tất cả tin nhắn gửi đến SUPPORT và tin nhắn từ SUPPORT trả lời
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    query = _context.Messages
                        .Where(m => (m.ReceiverUsername == "SUPPORT" || m.SenderUsername == "SUPPORT") &&
                                   (orderId == null || m.OrderId == orderId));
                }
                else
                {
                    // Nếu là khách hàng, lấy tin nhắn giữa họ và SUPPORT
                    query = _context.Messages
                        .Where(m => 
                            ((m.SenderUsername == currentUsername && m.ReceiverUsername == "SUPPORT") ||
                             (m.SenderUsername == "SUPPORT" && m.ReceiverUsername == currentUsername)) &&
                            (orderId == null || m.OrderId == orderId));
                }
            }
            else
            {
                // Chat thông thường giữa 2 người
                query = _context.Messages
                    .Where(m => 
                        ((m.SenderUsername == currentUsername && m.ReceiverUsername == otherUsername) ||
                         (m.SenderUsername == otherUsername && m.ReceiverUsername == currentUsername)) &&
                        (orderId == null || m.OrderId == orderId));
            }

            if (lastMessageId.HasValue)
            {
                query = query.Where(m => m.Id > lastMessageId.Value);
            }

            var messages = query
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    id = m.Id,
                    senderUsername = m.SenderUsername,
                    receiverUsername = m.ReceiverUsername,
                    content = m.Content,
                    sentAt = m.SentAt.ToString("dd/MM/yyyy HH:mm"),
                    isRead = m.IsRead,
                    isFromCurrentUser = m.SenderUsername == currentUsername,
                    messageType = m.MessageType
                })
                .ToList();

            // Đánh dấu tin nhắn đã đọc
            IQueryable<Message> unreadQuery;
            if (otherUsername == "SUPPORT")
            {
                if (User.IsInRole("Admin") || User.IsInRole("Staff"))
                {
                    // Admin/Staff không cần đánh dấu đọc tin nhắn từ SUPPORT
                    unreadQuery = _context.Messages.Where(m => false);
                }
                else
                {
                    // Khách hàng đánh dấu tin nhắn từ SUPPORT đã đọc
                    unreadQuery = _context.Messages
                        .Where(m => m.ReceiverUsername == currentUsername && 
                                   m.SenderUsername == "SUPPORT" && 
                                   !m.IsRead &&
                                   (orderId == null || m.OrderId == orderId));
                }
            }
            else
            {
                unreadQuery = _context.Messages
                    .Where(m => m.ReceiverUsername == currentUsername && 
                               m.SenderUsername == otherUsername && 
                               !m.IsRead &&
                               (orderId == null || m.OrderId == orderId));
            }
            
            var unreadMessages = unreadQuery.ToList();

            foreach (var msg in unreadMessages)
            {
                msg.IsRead = true;
                msg.ReadAt = DateTime.Now;
            }
            _context.SaveChanges();

            return Json(new { success = true, messages = messages });
        }

        // GET: Chat/GetUnreadCount
        [HttpGet]
        public IActionResult GetUnreadCount()
        {
            var currentUsername = User.Identity?.Name;
            if (string.IsNullOrEmpty(currentUsername))
            {
                return Json(new { count = 0 });
            }

            var count = _context.Messages
                .Count(m => m.ReceiverUsername == currentUsername && !m.IsRead);

            return Json(new { count = count });
        }
    }
}

