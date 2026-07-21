using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed record CreateOrderItem(int MenuItemId, int Quantity);

public sealed record CreateOrderRequest(
    string CustomerName,
    string Phone,
    string? Note,
    PaymentMethod PaymentMethod,
    IReadOnlyCollection<CreateOrderItem> Items);

public sealed class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message)
    {
    }
}

/// <summary>
/// Tạo đơn từ dữ liệu tin cậy trong DB. Giá và trạng thái còn hàng trong Session
/// không được sử dụng để quyết định đơn hàng cuối cùng.
/// </summary>
public sealed class OrderService
{
    private const int MaximumQuantityPerItem = 99;
    private readonly AppDbContext _db;

    public OrderService(AppDbContext db)
    {
        _db = db;
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
