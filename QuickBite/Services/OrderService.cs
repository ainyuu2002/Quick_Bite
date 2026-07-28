using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Models;
using QuickBite.Services.Events;

namespace QuickBite.Services;

public sealed record CreateOrderItem(int MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    string CustomerName,
    string Phone,
    string Address,
    string? Note,
    PaymentMethod PaymentMethod,
    IReadOnlyCollection<CreateOrderItem> Items);

public sealed class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message)
    {
    }
}

public sealed class OrderService
{
    private const int MaximumQuantityPerItem = 99;
    private readonly AppDbContext _db;
    private readonly IHubContext<OrderHub> _hub;
    private readonly IOrderEventPublisher _events;

    public OrderService(AppDbContext db, IHubContext<OrderHub> hub, IOrderEventPublisher events)
    {
        _db = db;
        _hub = hub;
        _events = events;
    }

    public async Task<Order> CreateOrderAsync(
        CreateOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new OrderValidationException("Giỏ hàng đang trống.");
        }

        if (!Enum.IsDefined(request.PaymentMethod))
        {
            throw new OrderValidationException("Phương thức thanh toán không hợp lệ.");
        }

        if (request.Items.Any(item => item.MenuItemId <= 0 || item.Quantity <= 0))
        {
            throw new OrderValidationException("Món ăn hoặc số lượng trong giỏ không hợp lệ.");
        }

        var normalizedItems = request.Items
            .GroupBy(item => item.MenuItemId)
            .Select(group => new CreateOrderItem(group.Key, group.Sum(item => item.Quantity)))
            .ToArray();

        if (normalizedItems.Any(item => item.Quantity > MaximumQuantityPerItem))
        {
            throw new OrderValidationException(
                $"Mỗi món chỉ được đặt tối đa {MaximumQuantityPerItem} phần trong một đơn.");
        }

        var menuItemIds = normalizedItems.Select(item => item.MenuItemId).ToArray();
        var menuItems = await _db.MenuItems
            .Where(item => menuItemIds.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);

        var invalidItems = normalizedItems
            .Where(item => !menuItems.TryGetValue(item.MenuItemId, out var menuItem)
                || !menuItem.IsAvailable)
            .Select(item => item.MenuItemId)
            .ToArray();

        if (invalidItems.Length > 0)
        {
            throw new OrderValidationException(
                "Một số món không còn phục vụ. Vui lòng quay lại giỏ hàng và chọn món khác.");
        }

        var order = new Order
        {
            CustomerName = request.CustomerName?.Trim() ?? string.Empty,
            Phone = request.Phone?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            PaymentMethod = request.PaymentMethod,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.Now
        };

        ValidateOrder(order);

        foreach (var item in normalizedItems)
        {
            var menuItem = menuItems[item.MenuItemId];
            order.Items.Add(new OrderItem
            {
                MenuItemId = menuItem.Id,
                Quantity = item.Quantity,
                UnitPrice = menuItem.Price
            });
        }

        order.Total = order.Items.Sum(item => item.LineTotal);
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);

        await _hub.Clients.Group("staff").SendAsync("NewOrder", new
        {
            id = order.Id,
            customerName = order.CustomerName,
            phone = order.Phone,
            total = order.Total,
            createdAt = order.CreatedAt,
            items = order.Items.Select(item => new
            {
                name = menuItems[item.MenuItemId].Name,
                quantity = item.Quantity
            })
        }, cancellationToken);

        return order;
    }

    public Task<Order?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
        => _db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.MenuItem)
            .SingleOrDefaultAsync(order => order.Id == orderId, cancellationToken);

    public Task<bool> HasOrdersByPhoneAsync(
        string phone,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        return _db.Orders
            .AsNoTracking()
            .AnyAsync(order => order.Phone == normalizedPhone, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> GetOrdersByPhoneAsync(
        string phone,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = phone.Trim();
        return await _db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.MenuItem)
            .Where(order => order.Phone == normalizedPhone)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<Order> CancelOrderAsync(
        int orderId,
        string customerPhone,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedPhone = customerPhone.Trim();
        var order = await _db.Orders
            .SingleOrDefaultAsync(
                item => item.Id == orderId && item.Phone == normalizedPhone,
                cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (!order.Status.CanBeCancelledByCustomer())
        {
            throw new OrderValidationException(
                "Khách hàng chỉ có thể hủy đơn khi quán chưa xác nhận.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var fromStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        AddStatusHistory(order, fromStatus, OrderStatus.Cancelled, trimmedReason, null);

        await _db.SaveChangesAsync(cancellationToken);

        await NotifyStatusChangedAsync(order, cancellationToken);
        await _events.PublishAsync(new OrderCancelled(order.Id, trimmedReason), cancellationToken);

        return order;
    }

    public async Task<Order> ChangeStatusAsync(
        int orderId,
        OrderStatus nextStatus,
        int? actorAccountId,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(nextStatus))
        {
            throw new OrderValidationException("Trạng thái đơn hàng không hợp lệ.");
        }

        var order = await _db.Orders
            .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (!order.Status.CanTransitionTo(nextStatus))
        {
            throw new OrderValidationException(
                $"Không thể chuyển đơn từ {order.Status.ToDisplayText()} " +
                $"sang {nextStatus.ToDisplayText()}.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        if (nextStatus.RequiresReason() && trimmedReason is null)
        {
            throw new OrderValidationException(
                $"Vui lòng chọn lý do khi chuyển đơn sang {nextStatus.ToDisplayText()}.");
        }

        if (nextStatus == OrderStatus.Confirmed && order.AcceptedByAccountId is null)
        {
            order.AcceptedByAccountId = actorAccountId;
            order.AcceptedAt = DateTime.Now;
        }

        var fromStatus = order.Status;
        order.Status = nextStatus;
        AddStatusHistory(order, fromStatus, nextStatus, trimmedReason, actorAccountId);

        await _db.SaveChangesAsync(cancellationToken);

        await NotifyStatusChangedAsync(order, cancellationToken);
        await PublishStatusEventAsync(order, trimmedReason, cancellationToken);

        return order;
    }

    private void AddStatusHistory(
        Order order,
        OrderStatus fromStatus,
        OrderStatus toStatus,
        string? reason,
        int? actorAccountId)
    {
        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            Reason = reason,
            ChangedByAccountId = actorAccountId,
            ChangedAt = DateTime.Now
        });
    }

    private Task PublishStatusEventAsync(Order order, string? reason, CancellationToken cancellationToken)
    {
        IOrderEvent? orderEvent = order.Status switch
        {
            OrderStatus.Confirmed => new OrderConfirmed(order.Id),
            OrderStatus.Completed => new OrderCompleted(order.Id),
            OrderStatus.Cancelled => new OrderCancelled(order.Id, reason),
            OrderStatus.Rejected => new OrderRejected(order.Id, reason),
            OrderStatus.Expired => new OrderExpired(order.Id),
            _ => null
        };

        return orderEvent is null
            ? Task.CompletedTask
            : _events.PublishAsync(orderEvent, cancellationToken);
    }

    private async Task NotifyStatusChangedAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        var payload = new
        {
            orderId = order.Id,
            status = (int)order.Status,
            statusText = order.Status.ToDisplayText()
        };

        await _hub.Clients.Group($"order-{order.Id}")
            .SendAsync("OrderStatusChanged", payload, cancellationToken);
        await _hub.Clients.Group("staff")
            .SendAsync("OrderStatusChanged", payload, cancellationToken);
    }

    private static void ValidateOrder(Order order)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(order);
        if (Validator.TryValidateObject(
            order,
            validationContext,
            validationResults,
            validateAllProperties: true))
        {
            return;
        }

        throw new OrderValidationException(
            string.Join(" ", validationResults.Select(result => result.ErrorMessage)));
    }
}
