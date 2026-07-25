using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Services;

public sealed class WorkSessionService
{
    private static readonly TimeSpan ReconnectGrace = TimeSpan.FromMinutes(5);

    private readonly AppDbContext _db;

    public WorkSessionService(AppDbContext db) => _db = db;

    public async Task OpenAsync(int accountId, CancellationToken cancellationToken = default)
    {
        var hasOpenSession = await _db.WorkSessions
            .AnyAsync(w => w.AccountId == accountId && w.CheckOutAt == null, cancellationToken);

        if (hasOpenSession)
        {
            return;
        }

        var now = DateTime.Now;

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
