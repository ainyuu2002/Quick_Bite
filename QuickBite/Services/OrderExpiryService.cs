using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed class OrderExpiryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly OrderingOptions _options;
    private readonly ILogger<OrderExpiryService> _logger;

    public OrderExpiryService(
        IServiceScopeFactory scopeFactory,
        IOptions<OrderingOptions> options,
        ILogger<OrderExpiryService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromSeconds(Math.Max(15, _options.SweepIntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Lỗi khi quét đơn quá hạn.");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var orderService = scope.ServiceProvider.GetRequiredService<OrderService>();

        var now = DateTime.Now;

        var pendingCutoff = now.AddMinutes(-_options.PendingExpiryMinutes);
        var expiredIds = await db.Orders
            .Where(order => order.Status == OrderStatus.Pending && order.CreatedAt < pendingCutoff)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        foreach (var orderId in expiredIds)
        {
            await TryChangeAsync(orderService, orderId, OrderStatus.Expired, null, cancellationToken);
        }

        var readyCutoff = now.AddMinutes(-_options.ReadyNoShowMinutes);
        var noShowIds = await db.Orders
            .Where(order => order.Status == OrderStatus.Ready && order.OrderType == OrderType.Pickup)
            .Select(order => new
            {
                order.Id,
                ReadyAt = db.OrderStatusHistories
                    .Where(history => history.OrderId == order.Id && history.ToStatus == OrderStatus.Ready)
                    .Max(history => (DateTime?)history.ChangedAt)
            })
            .Where(entry => entry.ReadyAt != null && entry.ReadyAt < readyCutoff)
            .Select(entry => entry.Id)
            .ToListAsync(cancellationToken);

        foreach (var orderId in noShowIds)
        {
            await TryChangeAsync(
                orderService,
                orderId,
                OrderStatus.NoShow,
                "Quá thời gian giữ đơn, khách không đến lấy.",
                cancellationToken);
        }

        var depositCutoff = now.AddMinutes(-_options.PartyDepositTimeoutMinutes);
        var depositExpiredIds = await db.Orders
            .Where(order => order.Status == OrderStatus.PendingReview
                && order.IsPartyOrder
                && order.ApprovedAt != null
                && !order.DepositPaid
                && order.ApprovedAt < depositCutoff)
            .Select(order => order.Id)
            .ToListAsync(cancellationToken);

        foreach (var orderId in depositExpiredIds)
        {
            await TryChangeAsync(
                orderService,
                orderId,
                OrderStatus.Expired,
                "Quá hạn đặt cọc đơn tiệc.",
                cancellationToken);
        }
    }

    private async Task TryChangeAsync(
        OrderService orderService,
        int orderId,
        OrderStatus nextStatus,
        string? reason,
        CancellationToken cancellationToken)
    {
        try
        {
            await orderService.ChangeStatusAsync(orderId, nextStatus, null, reason, cancellationToken);
        }
        catch (Exception exception) when (exception is OrderValidationException or KeyNotFoundException)
        {
            _logger.LogWarning(
                exception,
                "Không thể tự chuyển đơn #{OrderId} sang {Status}.",
                orderId,
                nextStatus);
        }
    }
}
