using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace webgiaohang.Hubs
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class LocationHub : Hub
    {
        // Shipper join group for specific order tracking
        public async Task JoinOrderGroup(int orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        // Leave order group
        public async Task LeaveOrderGroup(int orderId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        // Subscribe to shipper location updates for an order
        public async Task SubscribeToOrder(int orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order_{orderId}");
        }

        public override async Task OnConnectedAsync()
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                // Join user-specific group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"shipper_{username}");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var username = Context.User?.Identity?.Name;
            if (!string.IsNullOrEmpty(username))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"shipper_{username}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
