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

    /// <summary>Một dòng báo cáo cho một nhân viên trong ngày đã chọn.</summary>
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

        // Ca làm có giờ VÀO trong ngày. Ca vắt qua nửa đêm được tính theo ngày bắt đầu
        // — đủ dùng cho demo; không bổ đôi ca quanh mốc 00:00.
        var sessions = await _context.WorkSessions
            .AsNoTracking()
            .Include(w => w.Account)
            .Where(w => w.CheckInAt >= dayStart && w.CheckInAt < dayEnd)
            .ToListAsync(cancellationToken);

        // Đếm đơn đã nhận trong ngày, gom sẵn theo nhân viên để tránh N+1.
        var orderCounts = await _context.Orders
            .AsNoTracking()
            .Where(o => o.AcceptedByAccountId != null
                && o.AcceptedAt >= dayStart && o.AcceptedAt < dayEnd)
            .GroupBy(o => o.AcceptedByAccountId!.Value)
            .Select(g => new { AccountId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.AccountId, x => x.Count, cancellationToken);

        var now = DateTime.Now;

        // Nhân viên xuất hiện trong báo cáo nếu có ca HOẶC có nhận đơn trong ngày.
        var accountIds = sessions.Select(s => s.AccountId)
            .Union(orderCounts.Keys)
            .ToHashSet();

        var rows = new List<ReportRow>();
        foreach (var accountId in accountIds)
        {
            var mySessions = sessions.Where(s => s.AccountId == accountId).ToList();

            // Ca đang mở tính thời lượng tới hiện tại (chỉ đúng khi xem báo cáo hôm nay).
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
                // Còn ca đang mở thì "giờ ra" chưa xác định.
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

    /// <summary>Số nhân viên đang trực ngay lúc này (chỉ ý nghĩa khi xem báo cáo hôm nay).</summary>
    public int StaffOnlineNow => _connectionTracker.StaffOnline;

    // Trường hợp hiếm: nhân viên có nhận đơn trong ngày nhưng không có ca nào (ví dụ dữ
    // liệu seed). Lấy tên riêng để dòng không bị trống.
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
