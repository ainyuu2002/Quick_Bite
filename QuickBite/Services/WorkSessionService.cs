using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

/// <summary>
/// Ghi/đóng ca làm việc (WorkSession) xuống DB. Scoped — được OrderHub gọi mỗi khi
/// một tài khoản vào ca (kết nối đầu tiên) hoặc tan ca (kết nối cuối cùng ngắt).
/// Tách khỏi <see cref="ConnectionTracker"/> (Singleton) để phần chạm DB nằm ở service
/// scoped, đúng nguyên tắc vòng đời DI.
/// </summary>
public sealed class WorkSessionService
{
    // Ca vừa đóng cách đây chưa tới ngần này thì coi như KHÔNG rời ca thật, chỉ là
    // chuyển trang / reconnect — nối lại ca cũ thay vì đẻ ca mới. App là multi-page nên
    // mỗi lần điều hướng, kết nối SignalR chết rồi tạo lại; không có grace thì mỗi cú
    // chuyển tab thành một "ca", bảng phình rác và tổng giờ trực bị cắt vụn.
    private static readonly TimeSpan ReconnectGrace = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;

    public WorkSessionService(AppDbContext db) => _db = db;

    /// <summary>Mở ca cho tài khoản, hoặc nối lại ca vừa đóng nếu chỉ là chuyển trang.</summary>
    public async Task OpenAsync(int accountId, CancellationToken cancellationToken = default)
    {
        // Còn ca mở (nhiều tab, hoặc trạng thái lệch) → không mở thêm ca thứ hai.
        var hasOpenSession = await _db.WorkSessions
            .AnyAsync(w => w.AccountId == accountId && w.CheckOutAt == null, cancellationToken);

        if (hasOpenSession)
        {
            return;
        }

        var now = DateTime.Now;

        // Ca gần nhất vừa đóng — nếu còn trong khoảng ân hạn thì mở lại chính nó,
        // coi như liền mạch (khoảng gap ngắn được tính luôn vào giờ trực).
        var recentSession = await _db.WorkSessions
            .Where(w => w.AccountId == accountId && w.CheckOutAt != null)
            .OrderByDescending(w => w.CheckOutAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recentSession is not null
            && now - recentSession.CheckOutAt!.Value <= ReconnectGrace)
        {
            recentSession.CheckOutAt = null;
            await _db.SaveChangesAsync(cancellationToken);
            return;
        }

        _db.WorkSessions.Add(new WorkSession
        {
            AccountId = accountId,
            CheckInAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Đóng mọi ca đang mở của tài khoản (bình thường chỉ có đúng một).</summary>
    public async Task CloseAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var openSessions = await _db.WorkSessions
            .Where(w => w.AccountId == accountId && w.CheckOutAt == null)
            .ToListAsync(cancellationToken);

        if (openSessions.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        foreach (var session in openSessions)
        {
            session.CheckOutAt = now;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Đóng mọi ca còn treo trên toàn hệ thống — gọi một lần lúc app khởi động.
    /// Sau khi server tắt/restart, các kết nối SignalR cũ đã chết nhưng ca của chúng
    /// vẫn còn CheckOutAt = null. Đóng bằng thời điểm khởi động lại là mốc gần đúng
    /// nhất còn biết được (ta không có thời điểm kết nối thực sự rớt).
    /// </summary>
    public async Task CloseAllOpenAsync(CancellationToken cancellationToken = default)
    {
        var stale = await _db.WorkSessions
            .Where(w => w.CheckOutAt == null)
            .ToListAsync(cancellationToken);

        if (stale.Count == 0)
        {
            return;
        }

        var now = DateTime.Now;
        foreach (var session in stale)
        {
            session.CheckOutAt = now;
        }
        await _db.SaveChangesAsync(cancellationToken);
    }
}
