using Microsoft.AspNetCore.SignalR;
using QuickBite.Services;

namespace QuickBite.Hubs
{
    /// <summary>
    /// Hub theo SDS mục 3.2 — chỉ lo join/leave group.
    /// Sự kiện nghiệp vụ gửi từ PageModel/service qua IHubContext&lt;OrderHub&gt;, không gọi hub trực tiếp.
    /// </summary>
    public class OrderHub : Hub
    {
        private readonly ConnectionTracker _tracker;

        public OrderHub(ConnectionTracker tracker) => _tracker = tracker;

        /// <summary>Màn hình admin/staff gọi sau khi connect (admin-orders.js).</summary>
        public async Task JoinStaff()
        {
            _tracker.Add(Context.ConnectionId, "staff");
            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");
            // Bổ sung ngoài SDS gốc (đã ghi vào SDS 3.3): số màn hình staff online realtime
            await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
        }

        /// <summary>Khách mở trang theo dõi đơn gọi để nhận OrderStatusChanged của đơn đó (order-track.js).</summary>
        public Task WatchOrder(int orderId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _tracker.Remove(Context.ConnectionId);
            await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
