using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QuickBite.Data;
using QuickBite.Hubs;
using QuickBite.Models;
using QuickBite.Modules.Operations.MenuAvailability;
using QuickBite.Modules.Operations.Store;
using QuickBite.Services.Events;

namespace QuickBite.Services;

public sealed record CreateOrderItem(int MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    string CustomerName,
    string Phone,
    string? Address,
    string? Note,
    OrderType OrderType,
    PaymentMethod PaymentMethod,
    bool IsParty,
    DateTime? ScheduledFor,
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
    private const int MaximumQuantityPerItem = 10;
    private readonly AppDbContext _db;
    private readonly IHubContext<OrderHub> _hub;
    private readonly IDiscountService _discounts;
    private readonly IOrderEventPublisher _events;
    private readonly IOrderEvents _retention;
    private readonly IStoreAvailabilityService _storeAvailability;
    private readonly IMenuAvailabilityService _menuAvailability;
    private readonly OrderingOptions _options;

    public OrderService(
        AppDbContext db,
        IHubContext<OrderHub> hub,
        IDiscountService discounts,
        IOrderEventPublisher events,
        IOrderEvents retention,
        IStoreAvailabilityService storeAvailability,
        IMenuAvailabilityService menuAvailability,
        IOptions<OrderingOptions> options)
    {
        _db = db;
        _hub = hub;
        _discounts = discounts;
        _events = events;
        _retention = retention;
        _storeAvailability = storeAvailability;
        _menuAvailability = menuAvailability;
        _options = options.Value;
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

        if (!Enum.IsDefined(request.OrderType))
        {
            throw new OrderValidationException("Loại đơn không hợp lệ.");
        }

        if (request.OrderType == OrderType.Delivery && string.IsNullOrWhiteSpace(request.Address))
        {
            throw new OrderValidationException("Đơn giao hàng cần có địa chỉ nhận.");
        }

        if (request.Items.Any(item => item.MenuItemId <= 0 || item.Quantity <= 0))
        {
            throw new OrderValidationException("Món ăn hoặc số lượng trong giỏ không hợp lệ.");
        }

        var normalizedItems = request.Items
            .GroupBy(item => item.MenuItemId)
            .Select(group => new CreateOrderItem(group.Key, group.Sum(item => item.Quantity)))
            .ToArray();

        if (!request.IsParty && normalizedItems.Any(item => item.Quantity > MaximumQuantityPerItem))
        {
            throw new OrderValidationException(
                $"Mỗi món chỉ được đặt tối đa {MaximumQuantityPerItem} phần trong một đơn thường. " +
                "Số lượng lớn vui lòng dùng chức năng Đặt tiệc.");
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var storeStatus = await _storeAvailability.GetStatusAsync(
            cancellationToken: cancellationToken);
        if (!storeStatus.IsAcceptingOrders)
        {
            var message = storeStatus.IsPaused
                ? string.IsNullOrWhiteSpace(storeStatus.PauseReason)
                    ? "Quán đang tạm ngưng nhận đơn."
                    : $"Quán đang tạm ngưng nhận đơn: {storeStatus.PauseReason}"
                : $"Quán chỉ nhận đơn từ {storeStatus.OpensAt:HH\\:mm} đến {storeStatus.ClosesAt:HH\\:mm}.";
            throw new OrderValidationException(message);
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

        var normalizedPhone = request.Phone?.Trim() ?? string.Empty;

        var openOrderCount = await _db.Orders.CountAsync(
            existing => existing.Phone == normalizedPhone
                && (existing.Status == OrderStatus.Pending
                    || existing.Status == OrderStatus.PendingReview
                    || existing.Status == OrderStatus.Confirmed
                    || existing.Status == OrderStatus.Preparing
                    || existing.Status == OrderStatus.Ready
                    || existing.Status == OrderStatus.Delivering),
            cancellationToken);

        if (openOrderCount >= _options.MaxOpenOrdersPerPhone)
        {
            throw new OrderValidationException(
                $"Số điện thoại này đang có {openOrderCount} đơn chưa hoàn tất. " +
                "Vui lòng chờ xử lý xong trước khi đặt thêm.");
        }

        var subtotal = normalizedItems.Sum(item => menuItems[item.MenuItemId].Price * item.Quantity);

        DateTime? scheduledFor = null;
        var depositAmount = 0m;

        if (request.IsParty)
        {
            if (request.ScheduledFor is not DateTime scheduled)
            {
                throw new OrderValidationException("Đơn đặt tiệc cần chọn thời gian nhận.");
            }

            var earliest = DateTime.Now.AddHours(_options.PartyMinLeadHours);
            if (scheduled < earliest)
            {
                throw new OrderValidationException(
                    $"Đơn đặt tiệc phải hẹn nhận trước tối thiểu {_options.PartyMinLeadHours} giờ.");
            }

            scheduledFor = scheduled;
            depositAmount = Math.Round(subtotal * _options.PartyDepositPercent / 100m, 0);
        }
        else if (subtotal > _options.PartyThreshold)
        {
            throw new OrderValidationException(
                $"Đơn trên {_options.PartyThreshold:N0}đ tiền món vui lòng dùng chức năng Đặt tiệc (hẹn giờ trước và đặt cọc).");
        }

        if (request.OrderType == OrderType.Delivery && !request.IsParty
            && subtotal < _options.MinimumDeliverySubtotal)
        {
            throw new OrderValidationException(
                $"Đơn giao hàng tối thiểu {_options.MinimumDeliverySubtotal:N0}đ tiền món (chưa gồm phí giao).");
        }

        var quotaItems = normalizedItems
            .Select(item => new MenuQuotaRequest(item.MenuItemId, item.Quantity))
            .ToArray();
        var reservation = await _menuAvailability.TryReserveAsync(
            quotaItems,
            cancellationToken: cancellationToken);
        if (!reservation.Success)
        {
            throw new OrderValidationException(
                reservation.ErrorMessage ?? "Một số món hiện không thể nhận thêm đơn.");
        }

        var deliveryFee = request.OrderType == OrderType.Delivery ? _options.DeliveryFee : 0m;
        var orderCode = await GenerateUniqueOrderCodeAsync(cancellationToken);

        var order = new Order
        {
            CustomerName = request.CustomerName?.Trim() ?? string.Empty,
            Phone = normalizedPhone,
            Address = request.OrderType == OrderType.Pickup
                ? null
                : request.Address?.Trim(),
            Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
            OrderType = request.OrderType,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = !request.IsParty && request.PaymentMethod == PaymentMethod.BankTransfer
                ? PaymentStatus.Paid
                : PaymentStatus.Unpaid,
            DeliveryFee = deliveryFee,
            OrderCode = orderCode,
            IsPartyOrder = request.IsParty,
            ScheduledFor = scheduledFor,
            DepositAmount = depositAmount,
            Status = request.IsParty ? OrderStatus.PendingReview : OrderStatus.Pending,
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
        // Công thức hợp nhất: tiền món − giảm giá + phí giao (BR-03).
        order.Total = itemsTotal - quote.DiscountAmount + deliveryFee;
        _db.Orders.Add(order);
        await _db.SaveChangesAsync(cancellationToken);
        await _discounts.CommitAsync(order, quote, cancellationToken);
        await transaction.CommitAsync(cancellationToken);

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

    public Task<Order?> GetByCodeAsync(string orderCode, CancellationToken cancellationToken = default)
    {
        var normalizedCode = orderCode.Trim().ToUpperInvariant();
        return _db.Orders
            .AsNoTracking()
            .Include(order => order.Items)
            .ThenInclude(item => item.MenuItem)
            .SingleOrDefaultAsync(order => order.OrderCode == normalizedCode, cancellationToken);
    }

    public async Task<Order> CancelByCodeAsync(
        string orderCode,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedCode = orderCode.Trim().ToUpperInvariant();
        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var order = await _db.Orders
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.OrderCode == normalizedCode, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng với mã này.");

        if (!order.Status.CanBeCancelledByCustomer())
        {
            throw new OrderValidationException(
                "Chỉ có thể hủy đơn khi quán chưa xác nhận.");
        }

        var trimmedReason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        var fromStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        AddStatusHistory(order, fromStatus, OrderStatus.Cancelled, trimmedReason, null);

        await _db.SaveChangesAsync(cancellationToken);
        await ReleaseQuotaIfNeededAsync(order, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();

        await _retention.OrderCancelledAsync(order, cancellationToken);
        await NotifyStatusChangedAsync(order, cancellationToken);
        await _events.PublishAsync(new OrderCancelled(order.Id, trimmedReason), cancellationToken);

        return order;
    }

    public Task<string?> GetLatestReasonAsync(int orderId, CancellationToken cancellationToken = default)
        => _db.OrderStatusHistories
            .AsNoTracking()
            .Where(history => history.OrderId == orderId && history.Reason != null)
            .OrderByDescending(history => history.ChangedAt)
            .Select(history => history.Reason)
            .FirstOrDefaultAsync(cancellationToken);

    public Task<List<ReasonCatalog>> GetReasonsAsync(
        ReasonKind kind,
        CancellationToken cancellationToken = default)
        => _db.ReasonCatalogs
            .AsNoTracking()
            .Where(reason => reason.IsActive && reason.Kind == kind)
            .OrderBy(reason => reason.DisplayOrder)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlySet<string>> GetBlacklistedPhonesAsync(
        IEnumerable<string> phones,
        CancellationToken cancellationToken = default)
    {
        var normalized = phones.Select(phone => phone.Trim()).Distinct().ToArray();
        var matches = await _db.PhoneBlacklists
            .AsNoTracking()
            .Where(entry => normalized.Contains(entry.Phone))
            .Select(entry => entry.Phone)
            .ToListAsync(cancellationToken);
        return matches.ToHashSet();
    }

    private async Task<string> GenerateUniqueOrderCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = OrderCodeGenerator.Generate();
            var exists = await _db.Orders.AnyAsync(order => order.OrderCode == code, cancellationToken);
            if (!exists)
            {
                return code;
            }
        }

        return OrderCodeGenerator.Generate(8);
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

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);
        var order = await _db.Orders
            .Include(item => item.Items)
            .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (!order.Status.CanTransitionTo(nextStatus, order.OrderType))
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

        if (nextStatus == OrderStatus.Completed && order.PaymentStatus == PaymentStatus.Unpaid)
        {
            order.PaymentStatus = PaymentStatus.Paid;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await ReleaseQuotaIfNeededAsync(order, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await transaction.DisposeAsync();

        if (nextStatus == OrderStatus.Completed)
        {
            await _retention.OrderCompletedAsync(order, cancellationToken);
        }
        else if (nextStatus == OrderStatus.Cancelled)
        {
            await _retention.OrderCancelledAsync(order, cancellationToken);
        }

        await NotifyStatusChangedAsync(order, cancellationToken);
        await PublishStatusEventAsync(order, trimmedReason, cancellationToken);

        return order;
    }

    public async Task<Order> ApprovePartyAsync(
        int orderId,
        int? actorAccountId,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (!order.IsPartyOrder || order.Status != OrderStatus.PendingReview || order.ApprovedAt is not null)
        {
            throw new OrderValidationException("Đơn tiệc này không ở trạng thái chờ duyệt.");
        }

        order.ApprovedAt = DateTime.Now;
        if (order.AcceptedByAccountId is null)
        {
            order.AcceptedByAccountId = actorAccountId;
            order.AcceptedAt = DateTime.Now;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await NotifyStatusChangedAsync(order, cancellationToken);

        return order;
    }

    public async Task<Order> RecordDepositAsync(
        int orderId,
        int? actorAccountId,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (!order.IsPartyOrder || order.Status != OrderStatus.PendingReview || order.ApprovedAt is null)
        {
            throw new OrderValidationException("Đơn tiệc phải được duyệt trước khi ghi nhận cọc.");
        }

        if (!order.DepositPaid)
        {
            order.DepositPaid = true;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return await ChangeStatusAsync(orderId, OrderStatus.Confirmed, actorAccountId, null, cancellationToken);
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
            Reason = reason is { Length: > 300 } ? reason[..300] : reason,
            ChangedByAccountId = actorAccountId,
            ChangedAt = DateTime.Now
        });
    }

    private Task ReleaseQuotaIfNeededAsync(
        Order order,
        CancellationToken cancellationToken)
    {
        if (!order.Status.ShouldReleaseQuota())
        {
            return Task.CompletedTask;
        }

        var items = order.Items
            .Select(item => new MenuQuotaRequest(item.MenuItemId, item.Quantity))
            .ToArray();
        return _menuAvailability.ReleaseAsync(
            items,
            DateOnly.FromDateTime(order.CreatedAt),
            cancellationToken);
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

        await _hub.Clients.Group($"order-{order.OrderCode}")
            .SendAsync("OrderStatusChanged", payload, cancellationToken);

        foreach (var roleGroup in new[] { "staff", "kitchen", "shipper" })
        {
            await _hub.Clients.Group(roleGroup)
                .SendAsync("OrderStatusChanged", payload, cancellationToken);
        }
    }

    public async Task<Order> UpdateContactAsync(
        int orderId,
        string customerName,
        string phone,
        string? address,
        CancellationToken cancellationToken = default)
    {
        var order = await _db.Orders
            .SingleOrDefaultAsync(item => item.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy đơn hàng.");

        if (order.Status != OrderStatus.Pending)
        {
            throw new OrderValidationException(
                "Chỉ sửa được thông tin liên hệ khi đơn còn chờ xác nhận.");
        }

        var trimmedName = customerName?.Trim();
        var trimmedPhone = phone?.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            throw new OrderValidationException("Tên khách không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(trimmedPhone))
        {
            throw new OrderValidationException("Số điện thoại không được để trống.");
        }

        if (order.OrderType == OrderType.Delivery && string.IsNullOrWhiteSpace(address))
        {
            throw new OrderValidationException("Đơn giao hàng cần có địa chỉ.");
        }

        order.CustomerName = trimmedName;
        order.Phone = trimmedPhone;
        order.Address = order.OrderType == OrderType.Pickup ? null : address?.Trim();

        await _db.SaveChangesAsync(cancellationToken);

        return order;
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
