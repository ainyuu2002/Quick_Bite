using Microsoft.EntityFrameworkCore;
using QuickBite.Data;

namespace QuickBite.Modules.Operations.Store;

public sealed record StoreAvailabilitySnapshot(
    bool IsAcceptingOrders,
    bool IsWithinBusinessHours,
    bool IsPaused,
    TimeOnly OpensAt,
    TimeOnly ClosesAt,
    string? PauseReason,
    DateTime UpdatedAt);

/// <summary>
/// Nguồn sự thật duy nhất cho cờ Store.IsAcceptingOrders mà module Ordering chỉ đọc.
/// </summary>
public interface IStoreAvailabilityService
{
    Task<StoreAvailabilitySnapshot> GetStatusAsync(
        DateTime? localNow = null,
        CancellationToken cancellationToken = default);

    Task UpdateBusinessHoursAsync(
        TimeOnly opensAt,
        TimeOnly closesAt,
        int actorAccountId,
        CancellationToken cancellationToken = default);

    Task SetPausedAsync(
        bool isPaused,
        string? reason,
        int actorAccountId,
        CancellationToken cancellationToken = default);
}

public sealed class StoreAvailabilityService : IStoreAvailabilityService
{
    private readonly AppDbContext _db;

    public StoreAvailabilityService(AppDbContext db) => _db = db;

    public async Task<StoreAvailabilitySnapshot> GetStatusAsync(
        DateTime? localNow = null,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingAsync(cancellationToken);
        var now = localNow ?? DateTime.Now;
        var withinHours = setting.IsWithinBusinessHours(TimeOnly.FromDateTime(now));

        return new StoreAvailabilitySnapshot(
            IsAcceptingOrders: withinHours && !setting.IsPaused,
            IsWithinBusinessHours: withinHours,
            IsPaused: setting.IsPaused,
            OpensAt: setting.OpensAt,
            ClosesAt: setting.ClosesAt,
            PauseReason: setting.PauseReason,
            UpdatedAt: setting.UpdatedAt);
    }

    public async Task UpdateBusinessHoursAsync(
        TimeOnly opensAt,
        TimeOnly closesAt,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingAsync(cancellationToken);
        setting.OpensAt = opensAt;
        setting.ClosesAt = closesAt;
        MarkUpdated(setting, actorAccountId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPausedAsync(
        bool isPaused,
        string? reason,
        int actorAccountId,
        CancellationToken cancellationToken = default)
    {
        var setting = await GetSettingAsync(cancellationToken);
        setting.IsPaused = isPaused;
        setting.PauseReason = isPaused ? reason?.Trim() : null;
        MarkUpdated(setting, actorAccountId);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private Task<StoreSetting> GetSettingAsync(CancellationToken cancellationToken)
        => _db.StoreSettings.SingleAsync(
            setting => setting.Id == StoreSetting.SingletonId,
            cancellationToken);

    private static void MarkUpdated(StoreSetting setting, int actorAccountId)
    {
        setting.UpdatedAt = DateTime.Now;
        setting.UpdatedByAccountId = actorAccountId;
    }
}
