using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuickBite.Services;

namespace QuickBite.Hubs
{
    public class OrderHub : Hub
    {
        private readonly ConnectionTracker _tracker;
        private readonly WorkSessionService _workSessions;

        // WorkSessionService là scoped: mỗi lần SignalR gọi một method của hub, nó tạo một
        // DI scope riêng, nên inject service scoped (kèm AppDbContext) vào đây là hợp lệ.
        public OrderHub(ConnectionTracker tracker, WorkSessionService workSessions)
        {
            _tracker = tracker;
            _workSessions = workSessions;
        }

        /// <summary>
        /// Chỉ tài khoản đã đăng nhập mới vào được nhóm "staff".
        /// [Authorize] đặt ở MỨC METHOD, không phải mức class — vì khách vãng lai
        /// (chưa đăng nhập) vẫn phải gọi được WatchOrder để theo dõi đơn của họ.
        /// </summary>
        [Authorize]
        public async Task JoinStaff()
        {
            var accountId = GetAccountId();
            if (accountId is null)
            {
                return;     // không đọc được danh tính → không chấm công, không vào group
            }

            var username = Context.User?.Identity?.Name ?? string.Empty;

            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");

            // Chỉ kết nối ĐẦU TIÊN của tài khoản mới mở ca (0 → 1).
            var isFirstConnection = _tracker.Connect(Context.ConnectionId, accountId.Value, username);
            if (isFirstConnection)
            {
                await _workSessions.OpenAsync(accountId.Value);
            }

            await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
        }

        public Task WatchOrder(int orderId)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderId}");

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Chỉ kết nối CUỐI CÙNG của tài khoản mới đóng ca (1 → 0).
            // Trả null nếu người đó còn tab khác, hoặc đây là kết nối của khách vãng lai.
            var closedAccountId = _tracker.Disconnect(Context.ConnectionId);
            if (closedAccountId is not null)
            {
                await _workSessions.CloseAsync(closedAccountId.Value);
                await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private int? GetAccountId()
            => int.TryParse(
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : null;
    }
}
