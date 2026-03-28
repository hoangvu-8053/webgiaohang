using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace webgiaohang.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class ChatHub : Hub
    {
        public async Task JoinConversation(string conversationId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        public async Task LeaveConversation(string conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"conversation_{conversationId}");
        }

        public async Task SendMessage(string conversationId, string senderUsername, string receiverUsername, string content, int? orderId)
        {
            await Clients.Group($"conversation_{conversationId}").SendAsync("ReceiveMessage", new
            {
                senderUsername,
                receiverUsername,
                content,
                orderId,
                sentAt = DateTime.Now
            });

            // Notify receiver
            await Clients.Group($"user_{receiverUsername}").SendAsync("NewMessage", new
            {
                senderUsername,
                content,
                orderId
            });
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{username}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{username}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}

