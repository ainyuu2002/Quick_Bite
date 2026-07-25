using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Services;

namespace QuickBite.Pages.Admin.Staff;

public class ReportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ConnectionTracker _connectionTracker;

    public ReportModel(AppDbContext context, ConnectionTracker connectionTracker)
    {
        _context = context;
        _connectionTracker = connectionTracker;
    }

    public sealed record ReportRow(
        int AccountId,
        string Name,
        DateTime? FirstCheckIn,
        DateTime? LastCheckOut,
        TimeSpan TotalWorked,
        bool HasOpenSession,
        int AcceptedOrderCount);

    [BindProperty(SupportsGet = true)]
    public DateTime Date { get; set; } = DateTime.Today;

    public IReadOnlyList<ReportRow> Rows { get; private set; } = Array.Empty<ReportRow>();

    public bool IsToday => Date.Date == DateTime.Today;

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        var dayStart = Date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sessions = await _context.WorkSessions
            .AsNoTracking()
            .Include(w => w.Account)
            .Where(w => w.CheckInAt >= dayStart && w.CheckInAt < dayEnd)
            .ToListAsync(cancellationToken);

        var orderCounts = await _context.Orders
            .AsNoTracking()
            .Where(o => o.AcceptedByAccountId != null
                && o.AcceptedAt >= dayStart && o.AcceptedAt < dayEnd)
            .GroupBy(o => o.AcceptedByAccountId!.Value)
            .Select(g => new { AccountId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AccountId, x => x.Count, cancellationToken);

        var now = DateTime.Now;

        var accountIds = sessions.Select(s => s.AccountId)
            .Union(orderCounts.Keys)
            .ToHashSet();

        var rows = new List<ReportRow>();
        foreach (var accountId in accountIds)
        {
            var mySessions = sessions.Where(s => s.AccountId == accountId).ToList();

            var total = mySessions.Aggregate(TimeSpan.Zero,
                (sum, s) => sum + ((s.CheckOutAt ?? now) - s.CheckInAt));

            var hasOpenSession = mySessions.Any(s => s.CheckOutAt is null);

            var name = mySessions.FirstOrDefault()?.Account is { } acc
                ? (acc.FullName ?? acc.Username)
                : await ResolveNameAsync(accountId, cancellationToken);

            rows.Add(new ReportRow(
                accountId,
                name,
                mySessions.Count == 0 ? null : mySessions.Min(s => s.CheckInAt),
                hasOpenSession || mySessions.Count == 0
                    ? null
                    : mySessions.Max(s => s.CheckOutAt),
                total,
                hasOpenSession,
                orderCounts.GetValueOrDefault(accountId)));
        }

        Rows = rows
            .OrderByDescending(r => r.TotalWorked)
            .ThenByDescending(r => r.AcceptedOrderCount)
            .ToList();
    }

   
    public int StaffOnlineNow => _connectionTracker.StaffOnline;

    private async Task<string> ResolveNameAsync(int accountId, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == accountId, cancellationToken);
        return account?.FullName ?? account?.Username ?? $"#{accountId}";
    }

    public static string FormatDuration(TimeSpan span)
    {
        var hours = (int)span.TotalHours;
        var minutes = span.Minutes;
        if (hours > 0)
        {
            return $"{hours} giờ {minutes} phút";
        }
        return minutes > 0 ? $"{minutes} phút" : $"{span.Seconds} giây";
    }
}
