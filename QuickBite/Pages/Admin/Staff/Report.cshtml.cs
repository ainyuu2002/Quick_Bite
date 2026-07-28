using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Modules.Operations.Workforce;
using QuickBite.Services;

namespace QuickBite.Pages.Admin.Staff;

public sealed class ReportModel : PageModel
{
    private readonly AppDbContext _context;
    private readonly ConnectionTracker _connectionTracker;
    private readonly IWorkSessionApprovalService _approvalService;

    public ReportModel(
        AppDbContext context,
        ConnectionTracker connectionTracker,
        IWorkSessionApprovalService approvalService)
    {
        _context = context;
        _connectionTracker = connectionTracker;
        _approvalService = approvalService;
    }

    public sealed record ReportRow(
        int AccountId,
        string Name,
        TimeSpan MachineTotal,
        TimeSpan ApprovedTotal,
        decimal ApprovedSalary,
        int DraftSessionCount,
        int ApprovedSessionCount,
        bool HasOpenSession,
        int AcceptedOrderCount);

    public sealed record SessionRow(
        int Id,
        string AccountName,
        DateTime CheckInAt,
        DateTime? CheckOutAt,
        WorkSessionApprovalStatus ApprovalStatus,
        DateTime? ApprovedCheckInAt,
        DateTime? ApprovedCheckOutAt,
        decimal? ApprovedHourlyRate,
        decimal? ApprovedSalary,
        string? ApprovalNote);

    public sealed class ApprovalInput
    {
        [Range(1, int.MaxValue)]
        public int WorkSessionId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giờ vào được duyệt.")]
        public DateTime? ApprovedCheckInAt { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giờ ra được duyệt.")]
        public DateTime? ApprovedCheckOutAt { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú tối đa 500 ký tự.")]
        public string? Note { get; set; }
    }

    [BindProperty(SupportsGet = true)]
    public DateTime Date { get; set; } = DateTime.Today;

    [BindProperty]
    public ApprovalInput Input { get; set; } = new();

    public IReadOnlyList<ReportRow> Rows { get; private set; } = Array.Empty<ReportRow>();

    public IReadOnlyList<SessionRow> Sessions { get; private set; } = Array.Empty<SessionRow>();

    public bool IsToday => Date.Date == DateTime.Today;

    public int StaffOnlineNow => _connectionTracker.StaffOnline;

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
        => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostApproveAsync(
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = string.Join(
                " ",
                ModelState.Values.SelectMany(value => value.Errors)
                    .Select(error => error.ErrorMessage));
            return RedirectToPage(new { Date = Date.ToString("yyyy-MM-dd") });
        }

        try
        {
            await _approvalService.ApproveAsync(
                Input.WorkSessionId,
                Input.ApprovedCheckInAt!.Value,
                Input.ApprovedCheckOutAt!.Value,
                Input.Note,
                GetCurrentAccountId(),
                cancellationToken);
        }
        catch (WorkSessionApprovalException exception)
        {
            TempData["ErrorMessage"] = exception.Message;
            return RedirectToPage(new { Date = Date.ToString("yyyy-MM-dd") });
        }

        TempData["SuccessMessage"] = "Đã chốt duyệt ca và tính lương.";
        return RedirectToPage(new { Date = Date.ToString("yyyy-MM-dd") });
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var dayStart = Date.Date;
        var dayEnd = dayStart.AddDays(1);

        var sessions = await _context.WorkSessions
            .AsNoTracking()
            .Include(item => item.Account)
            .Where(item => item.CheckInAt >= dayStart && item.CheckInAt < dayEnd)
            .OrderByDescending(item => item.CheckInAt)
            .ToListAsync(cancellationToken);

        var orderCounts = await _context.Orders
            .AsNoTracking()
            .Where(order => order.AcceptedByAccountId != null
                && order.AcceptedAt >= dayStart
                && order.AcceptedAt < dayEnd)
            .GroupBy(order => order.AcceptedByAccountId!.Value)
            .Select(group => new { AccountId = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.AccountId, item => item.Count, cancellationToken);

        var now = DateTime.Now;
        var accountIds = sessions.Select(item => item.AccountId)
            .Union(orderCounts.Keys)
            .ToArray();
        var fallbackNames = await _context.Accounts
            .AsNoTracking()
            .Where(account => accountIds.Contains(account.Id))
            .ToDictionaryAsync(
                account => account.Id,
                account => account.FullName ?? account.Username,
                cancellationToken);

        Rows = accountIds.Select(accountId =>
        {
            var accountSessions = sessions
                .Where(item => item.AccountId == accountId)
                .ToArray();
            var machineTotal = accountSessions.Aggregate(
                TimeSpan.Zero,
                (sum, item) => sum + ((item.CheckOutAt ?? now) - item.CheckInAt));
            var approved = accountSessions
                .Where(item => item.ApprovalStatus == WorkSessionApprovalStatus.Approved)
                .ToArray();

            return new ReportRow(
                accountId,
                fallbackNames.GetValueOrDefault(accountId, $"#{accountId}"),
                machineTotal,
                approved.Aggregate(
                    TimeSpan.Zero,
                    (sum, item) => sum + (item.ApprovedDuration ?? TimeSpan.Zero)),
                approved.Sum(item => item.ApprovedSalary ?? 0m),
                accountSessions.Count(item =>
                    item.ApprovalStatus == WorkSessionApprovalStatus.Draft),
                approved.Length,
                accountSessions.Any(item => item.CheckOutAt is null),
                orderCounts.GetValueOrDefault(accountId));
        })
        .OrderByDescending(item => item.ApprovedSalary)
        .ThenByDescending(item => item.MachineTotal)
        .ToArray();

        Sessions = sessions.Select(item => new SessionRow(
            item.Id,
            item.Account.FullName ?? item.Account.Username,
            item.CheckInAt,
            item.CheckOutAt,
            item.ApprovalStatus,
            item.ApprovedCheckInAt,
            item.ApprovedCheckOutAt,
            item.ApprovedHourlyRate,
            item.ApprovedSalary,
            item.ApprovalNote))
            .ToArray();
    }

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Không xác định được tài khoản hiện tại.");

    public static string FormatDuration(TimeSpan span)
    {
        var hours = (int)span.TotalHours;
        var minutes = span.Minutes;
        return hours > 0
            ? $"{hours} giờ {minutes} phút"
            : minutes > 0 ? $"{minutes} phút" : $"{span.Seconds} giây";
    }
}
