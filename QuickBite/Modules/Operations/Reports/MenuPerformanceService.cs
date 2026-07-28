using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Modules.Operations.Store;

namespace QuickBite.Modules.Operations.Reports;

public readonly record struct SaleTimeWindow(TimeOnly? StartsAt, TimeOnly? EndsAt)
{
    public bool IsAllDay => StartsAt is null || EndsAt is null || StartsAt == EndsAt;

    public bool Contains(TimeOnly time)
    {
        if (IsAllDay)
        {
            return true;
        }

        return StartsAt < EndsAt
            ? time >= StartsAt && time < EndsAt
            : time >= StartsAt || time < EndsAt;
    }
}

public sealed record MenuPerformanceRow(
    int MenuItemId,
    string Name,
    int Quantity,
    decimal GrossItemRevenue,
    DateTime CreatedAt);

public sealed record MenuPerformanceResult(
    IReadOnlyList<MenuPerformanceRow> ByQuantity,
    IReadOnlyList<MenuPerformanceRow> ByRevenue,
    IReadOnlyList<MenuPerformanceRow> SlowItems);

public interface IMenuPerformanceService
{
    Task<MenuPerformanceResult> GetReportAsync(
        DateTime from,
        DateTime toExclusive,
        SaleTimeWindow saleWindow,
        int slowItemThreshold,
        CancellationToken cancellationToken = default);

    Task<IReadOnlySet<int>> GetBestSellerIdsAsync(
        CancellationToken cancellationToken = default);

    Task UpdateSettingsAsync(
        int slowItemThreshold,
        int bestSellerTopCount,
        int actorAccountId,
        CancellationToken cancellationToken = default);
}

public sealed class MenuPerformanceService : IMenuPerformanceService
{
    private readonly AppDbContext _db;

    public MenuPerformanceService(AppDbContext db) => _db = db;

    public async Task<MenuPerformanceResult> GetReportAsync(
        DateTime from,
        DateTime toExclusive,
        SaleTimeWindow saleWindow,
        int slowItemThreshold,
        CancellationToken cancellationToken = default)
    {
        var selectedSales = await BuildSalesQuery(from, toExclusive, saleWindow)
            .ToListAsync(cancellationToken);

        var slowFrom = DateTime.Today.AddDays(-30);
        var slowSales = await BuildSalesQuery(slowFrom, DateTime.Now, saleWindow)
            .ToDictionaryAsync(item => item.MenuItemId, cancellationToken);
        var allMenuItems = await _db.MenuItems
            .AsNoTracking()
            .Select(item => new { item.Id, item.Name, item.CreatedAt })
            .ToListAsync(cancellationToken);

        var byQuantity = selectedSales
            .OrderByDescending(item => item.Quantity)
            .ThenByDescending(item => item.GrossItemRevenue)
            .ThenBy(item => item.Name)
            .ToArray();
        var byRevenue = selectedSales
            .OrderByDescending(item => item.GrossItemRevenue)
            .ThenByDescending(item => item.Quantity)
            .ThenBy(item => item.Name)
            .ToArray();
        var slowItems = allMenuItems
            .Select(item =>
            {
                slowSales.TryGetValue(item.Id, out var sale);
                return new MenuPerformanceRow(
                    item.Id,
                    item.Name,
                    sale?.Quantity ?? 0,
                    sale?.GrossItemRevenue ?? 0m,
                    item.CreatedAt);
            })
            .Where(item => item.Quantity < slowItemThreshold)
            .OrderBy(item => item.Quantity)
            .ThenBy(item => item.Name)
            .ToArray();

        return new MenuPerformanceResult(byQuantity, byRevenue, slowItems);
    }

    public async Task<IReadOnlySet<int>> GetBestSellerIdsAsync(
        CancellationToken cancellationToken = default)
    {
        var setting = await _db.StoreSettings
            .AsNoTracking()
            .SingleAsync(
                item => item.Id == StoreSetting.SingletonId,
                cancellationToken);
        var from = DateTime.Today.AddDays(-30);

        var ids = await _db.OrderItems
            .AsNoTracking()
            .Where(item => item.Order!.Status == OrderStatus.Completed
                && item.Order.CreatedAt >= from)
            .GroupBy(item => item.MenuItemId)
            .Select(group => new
            {
                MenuItemId = group.Key,
                Quantity = group.Sum(item => item.Quantity)
            })
            .OrderByDescending(item => item.Quantity)
            .ThenBy(item => item.MenuItemId)
            .Take(setting.BestSellerTopCount)
            .Select(item => item.MenuItemId)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }

    public async Task UpdateSettingsAsync(
        int slowItemThreshold,
        int bestSellerTopCount,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        if (slowItemThreshold is < 1 or > 1_000)
        {
            throw new ArgumentOutOfRangeException(nameof(slowItemThreshold));
        }
        if (bestSellerTopCount is < 1 or > 20)
        {
            throw new ArgumentOutOfRangeException(nameof(bestSellerTopCount));
        }

        var setting = await _db.StoreSettings.SingleAsync(
            item => item.Id == StoreSetting.SingletonId,
            cancellationToken);
        setting.SlowItemThreshold = slowItemThreshold;
        setting.BestSellerTopCount = bestSellerTopCount;
        setting.UpdatedAt = DateTime.Now;
        setting.UpdatedByAccountId = actorAccountId;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<MenuPerformanceRow> BuildSalesQuery(
        DateTime from,
        DateTime toExclusive,
        SaleTimeWindow saleWindow)
    {
        var query = _db.OrderItems
            .AsNoTracking()
            .Where(item => item.Order!.Status == OrderStatus.Completed
                && item.Order.CreatedAt >= from
                && item.Order.CreatedAt < toExclusive);

        if (!saleWindow.IsAllDay)
        {
            var startsAt = saleWindow.StartsAt!.Value.ToTimeSpan();
            var endsAt = saleWindow.EndsAt!.Value.ToTimeSpan();
            query = startsAt < endsAt
                ? query.Where(item =>
                    item.Order!.CreatedAt.TimeOfDay >= startsAt
                    && item.Order.CreatedAt.TimeOfDay < endsAt)
                : query.Where(item =>
                    item.Order!.CreatedAt.TimeOfDay >= startsAt
                    || item.Order.CreatedAt.TimeOfDay < endsAt);
        }

        return query
            .GroupBy(item => new
            {
                item.MenuItemId,
                item.MenuItem!.Name,
                item.MenuItem.CreatedAt
            })
            .Select(group => new MenuPerformanceRow(
                group.Key.MenuItemId,
                group.Key.Name,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.Quantity * item.UnitPrice),
                group.Key.CreatedAt));
    }
}
