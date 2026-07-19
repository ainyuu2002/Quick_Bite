using Microsoft.AspNetCore.SignalR;
using QuickBite.Services;
namespace QuickBite.Hubs
{
    public class OrderHub : Hub
    {
        private readonly ConnectionTracker _tracker;
        public OrderHub(ConnectionTracker tracker)
        {
            _tracker = tracker;
        }
        public override async Task OnConnectedAsync()
        {
            var role = Context.GetHttpContext()?.Request.Query["role"].ToString();
            if (string.IsNullOrEmpty(role))
                role = "customer"; // Default role if not provided
            _tracker.Add(Context.ConnectionId, role);
            if (role == "staff")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
            }
            await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
            await base.OnConnectedAsync();
        }

        /// <summary>Khách gọi sau khi connect để nhận "OrderStatusChanged" của đúng đơn mình.</summary>
        public async Task JoinOrderGroup(int orderId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _tracker.Remove(Context.ConnectionId);
            await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
