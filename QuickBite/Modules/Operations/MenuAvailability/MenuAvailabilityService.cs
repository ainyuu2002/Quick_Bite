using System.Data;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Modules.Operations.MenuAvailability;

public sealed record MenuQuotaRequest(int MenuItemId, int Quantity);

public sealed record MenuAvailabilitySnapshot(
    int MenuItemId,
    string MenuItemName,
    bool IsManuallyAvailable,
    bool IsWithinSaleWindow,
    int DailyLimit,
    int ReservedQuantity,
    int RemainingQuantity,
    TimeOnly? SaleStartsAt,
    TimeOnly? SaleEndsAt,
    bool IsAvailable);

public sealed record QuotaReservationResult(bool Success, string? ErrorMessage)
{
    public static QuotaReservationResult Accepted() => new(true, null);

    public static QuotaReservationResult Rejected(string message) => new(false, message);
}

/// <summary>
/// Hợp đồng cho module Ordering: đọc trạng thái, giữ quota khi tạo Pending và nhả
/// đúng ngày giữ chỗ khi đơn bị hủy/từ chối/hết hạn.
/// </summary>
public interface IMenuAvailabilityService
{
    Task<IReadOnlyList<MenuAvailabilitySnapshot>> GetStatusesAsync(
        DateTime? localNow = null,
        CancellationToken cancellationToken = default);

    Task<QuotaReservationResult> TryReserveAsync(
        IReadOnlyCollection<MenuQuotaRequest> items,
        DateTime? localNow = null,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        IReadOnlyCollection<MenuQuotaRequest> items,
        DateOnly reservationDate,
        CancellationToken cancellationToken = default);

    Task ConfigureAsync(
        int menuItemId,
        int dailyLimit,
        TimeOnly? saleStartsAt,
        TimeOnly? saleEndsAt,
        int actorAccountId,
        CancellationToken cancellationToken = default);
}

public sealed class MenuAvailabilityService : IMenuAvailabilityService
{
    private readonly AppDbContext _db;

    public MenuAvailabilityService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<MenuAvailabilitySnapshot>> GetStatusesAsync(
        DateTime? localNow = null,
        CancellationToken cancellationToken = default)
    {
        var now = localNow ?? DateTime.Now;
        var businessDate = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        var items = await _db.MenuItems
            .AsNoTracking()
            .OrderBy(item => item.Name)
            .Select(item => new
            {
                item.Id,
                item.Name,
                item.IsAvailable,
                Quota = _db.DailyQuotas.SingleOrDefault(quota => quota.MenuItemId == item.Id)
            })
            .ToListAsync(cancellationToken);

        return items.Select(item =>
        {
            var limit = item.Quota?.DailyLimit ?? DailyQuota.DefaultDailyLimit;
            var reserved = item.Quota?.QuotaDate == businessDate
                ? item.Quota.ReservedQuantity
                : 0;
            var withinWindow = item.Quota?.IsWithinSaleWindow(currentTime) ?? true;
            var remaining = Math.Max(0, limit - reserved);

            return new MenuAvailabilitySnapshot(
                item.Id,
                item.Name,
                item.IsAvailable,
                withinWindow,
                limit,
                reserved,
                remaining,
                item.Quota?.SaleStartsAt,
                item.Quota?.SaleEndsAt,
                item.IsAvailable && withinWindow && remaining > 0);
        }).ToArray();
    }

    public async Task<QuotaReservationResult> TryReserveAsync(
        IReadOnlyCollection<MenuQuotaRequest> items,
        DateTime? localNow = null,
        CancellationToken cancellationToken = default)
    {
        var normalizedItems = Normalize(items);
        if (normalizedItems.Count == 0)
        {
            return QuotaReservationResult.Rejected("Giỏ hàng không có món hợp lệ.");
        }

        var now = localNow ?? DateTime.Now;
        var businessDate = DateOnly.FromDateTime(now);
        var currentTime = TimeOnly.FromDateTime(now);

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var ids = normalizedItems.Keys.ToArray();
        var menuItems = await _db.MenuItems
            .Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        var quotas = await _db.DailyQuotas
            .Where(quota => ids.Contains(quota.MenuItemId))
            .ToDictionaryAsync(quota => quota.MenuItemId, cancellationToken);
        var persistedQuotaIds = quotas.Keys.ToHashSet();

        foreach (var (menuItemId, quantity) in normalizedItems)
        {
            if (!menuItems.TryGetValue(menuItemId, out var menuItem)
                || !menuItem.IsAvailable)
            {
                return QuotaReservationResult.Rejected(
                    "Một số món đã ngừng phục vụ.");
            }

            if (!quotas.TryGetValue(menuItemId, out var quota))
            {
                quota = new DailyQuota
                {
                    MenuItemId = menuItemId,
                    DailyLimit = DailyQuota.DefaultDailyLimit,
                    QuotaDate = businessDate,
                    UpdatedAt = now
                };
                quotas.Add(menuItemId, quota);
            }

            if (!quota.IsWithinSaleWindow(currentTime))
            {
                return QuotaReservationResult.Rejected(
                    $"{menuItem.Name} hiện ngoài khung giờ bán.");
            }

            var reservedToday = quota.QuotaDate == businessDate
                ? quota.ReservedQuantity
                : 0;
            var remainingToday = Math.Max(0, quota.DailyLimit - reservedToday);
            if (remainingToday < quantity)
            {
                return QuotaReservationResult.Rejected(
                    $"{menuItem.Name} chỉ còn {remainingToday} phần hôm nay.");
            }
        }

        foreach (var (menuItemId, quantity) in normalizedItems)
        {
            var quota = quotas[menuItemId];
            quota.ResetFor(businessDate);
            quota.ReservedQuantity += quantity;
            if (!persistedQuotaIds.Contains(menuItemId))
            {
                _db.DailyQuotas.Add(quota);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return QuotaReservationResult.Accepted();
    }

    public async Task ReleaseAsync(
        IReadOnlyCollection<MenuQuotaRequest> items,
        DateOnly reservationDate,
        CancellationToken cancellationToken = default)
    {
        var normalizedItems = Normalize(items);
        if (normalizedItems.Count == 0)
        {
            return;
        }

        await using var transaction = await _db.Database.BeginTransactionAsync(
            IsolationLevel.Serializable,
            cancellationToken);

        var ids = normalizedItems.Keys.ToArray();
        var quotas = await _db.DailyQuotas
            .Where(quota => ids.Contains(quota.MenuItemId)
                && quota.QuotaDate == reservationDate)
            .ToDictionaryAsync(quota => quota.MenuItemId, cancellationToken);

        foreach (var (menuItemId, quantity) in normalizedItems)
        {
            if (quotas.TryGetValue(menuItemId, out var quota))
            {
                quota.ReservedQuantity = Math.Max(0, quota.ReservedQuantity - quantity);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ConfigureAsync(
        int menuItemId,
        int dailyLimit,
        TimeOnly? saleStartsAt,
        TimeOnly? saleEndsAt,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        if (dailyLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dailyLimit));
        }

        if ((saleStartsAt is null) != (saleEndsAt is null))
        {
            throw new ArgumentException("Giờ bắt đầu và kết thúc phải được nhập cùng nhau.");
        }

        var menuItemExists = await _db.MenuItems
            .AnyAsync(item => item.Id == menuItemId, cancellationToken);
        if (!menuItemExists)
        {
            throw new KeyNotFoundException("Không tìm thấy món ăn.");
        }

        var quota = await _db.DailyQuotas
            .SingleOrDefaultAsync(item => item.MenuItemId == menuItemId, cancellationToken);
        if (quota is null)
        {
            quota = new DailyQuota
            {
                MenuItemId = menuItemId,
                QuotaDate = DateOnly.FromDateTime(DateTime.Today)
            };
            _db.DailyQuotas.Add(quota);
        }

        quota.DailyLimit = dailyLimit;
        quota.SaleStartsAt = saleStartsAt;
        quota.SaleEndsAt = saleEndsAt;
        quota.UpdatedAt = DateTime.Now;
        quota.UpdatedByAccountId = actorAccountId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static Dictionary<int, int> Normalize(IReadOnlyCollection<MenuQuotaRequest> items)
        => items
            .Where(item => item.MenuItemId > 0 && item.Quantity > 0)
            .GroupBy(item => item.MenuItemId)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
}
