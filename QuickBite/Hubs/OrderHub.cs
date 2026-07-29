using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Hubs
{
    public class OrderHub : Hub
    {
        private readonly ConnectionTracker _tracker;
        private readonly WorkSessionService _workSessions;

        public OrderHub(ConnectionTracker tracker, WorkSessionService workSessions)
        {
            _tracker = tracker;
            _workSessions = workSessions;
        }

        [Authorize]
        public async Task JoinStaff()
        {
            if (!InAnyRole(AccountRole.Manager, AccountRole.Staff))
            {
                return;
            }

            var accountId = GetAccountId();
            if (accountId is null)
            {
                return;
            }

            var username = Context.User?.Identity?.Name ?? string.Empty;

            await Groups.AddToGroupAsync(Context.ConnectionId, "staff");

            var isFirstConnection = _tracker.Connect(Context.ConnectionId, accountId.Value, username);
            if (isFirstConnection)
            {
                await _workSessions.OpenAsync(accountId.Value);
            }

            await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
        }

        [Authorize]
        public Task JoinKitchen()
            => InAnyRole(AccountRole.Manager, AccountRole.Kitchen)
                ? Groups.AddToGroupAsync(Context.ConnectionId, "kitchen")
                : Task.CompletedTask;

        [Authorize]
        public Task JoinShipper()
            => InAnyRole(AccountRole.Manager, AccountRole.Shipper)
                ? Groups.AddToGroupAsync(Context.ConnectionId, "shipper")
                : Task.CompletedTask;

        public Task WatchOrder(string orderCode)
            => Groups.AddToGroupAsync(Context.ConnectionId, $"order-{orderCode}");

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var closedAccountId = _tracker.Disconnect(Context.ConnectionId);
            if (closedAccountId is not null)
            {
                await _workSessions.CloseAsync(closedAccountId.Value);
                await Clients.Group("staff").SendAsync("StaffOnlineChanged", _tracker.StaffOnline);
            }

            await base.OnDisconnectedAsync(exception);
        }

        private bool InAnyRole(params AccountRole[] roles)
            => roles.Any(role => Context.User?.IsInRole(role.ToString()) == true);

        private int? GetAccountId()
            => int.TryParse(
                Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id)
                ? id
                : null;
    }
}
