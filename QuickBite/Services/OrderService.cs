using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed record CreateOrderItem(int MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    string CustomerName,
    string Phone,
    string Address,
    string? Note,
    PaymentMethod PaymentMethod,
    IReadOnlyCollection<CreateOrderItem> Items,
    int? CustomerId = null,
    string? PromotionCode = null,
    string? VoucherCode = null);

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
    private readonly IDiscountService _discounts;
    private readonly IOrderEvents _events;

    public OrderService(
        AppDbContext db,
        IHubContext<OrderHub> hub,
        IDiscountService discounts,
        IOrderEvents events)
    {
        _db = db;
        _hub = hub;
        _discounts = discounts;
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

        var itemsTotal = order.Items.Sum(item => item.LineTotal);
        var quote = await _discounts.QuoteAsync(
            order.Phone,
            request.CustomerId,
            itemsTotal,
            request.PromotionCode,
            request.VoucherCode,
            cancellationToken);

        order.CustomerId = request.CustomerId;
        order.PromotionId = quote.Promotion?.Id;
        order.VoucherId = quote.Voucher?.Id;
        order.DiscountAmount = quote.DiscountAmount;
        order.Total = itemsTotal - quote.DiscountAmount;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        await _discounts.CommitAsync(order, quote, cancellationToken);

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

        order.Status = OrderStatus.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);

        await _events.OrderCancelledAsync(order, cancellationToken);
        await NotifyStatusChangedAsync(order, cancellationToken);

        return order;
    }

    public async Task<Order> ChangeStatusAsync(
        int orderId,
        OrderStatus nextStatus,
        int? actorAccountId,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(nextStatus))
        {
            throw new OrderValidationException("Trạng thái đơn hàng không hợp lệ.");
        }

        var order = await _db.Orders
            .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        var isCancellation = nextStatus == OrderStatus.Cancelled
            && order.Status.CanBeCancelledByStaff();
        var isForwardTransition = order.Status.CanTransitionTo(nextStatus);

        if (!isCancellation && !isForwardTransition)
        {
            throw new OrderValidationException(
                $"Không thể chuyển đơn từ {order.Status.ToDisplayText()} " +
                $"sang {nextStatus.ToDisplayText()}.");
        }

        if (nextStatus == OrderStatus.Accepted && order.AcceptedByAccountId is null)
        {
            order.AcceptedByAccountId = actorAccountId;
            order.AcceptedAt = DateTime.Now;
        }

        order.Status = nextStatus;

        await _db.SaveChangesAsync(cancellationToken);

        if (nextStatus == OrderStatus.Completed)
        {
            await _events.OrderCompletedAsync(order, cancellationToken);
        }
        else if (nextStatus == OrderStatus.Cancelled)
        {
            await _events.OrderCancelledAsync(order, cancellationToken);
        }

        await NotifyStatusChangedAsync(order, cancellationToken);

        return order;
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
