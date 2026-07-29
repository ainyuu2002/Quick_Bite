using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using System.Security.Claims;

namespace QuickBite.Pages.Admin.Staff;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public sealed record StaffRow(Account Account, int AcceptedOrderCount);

    public IReadOnlyList<StaffRow> Rows { get; private set; } = Array.Empty<StaffRow>();

    public int CurrentAccountId { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        CurrentAccountId = GetCurrentAccountId();

        Rows = await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.Role)
            .ThenBy(a => a.Username)
            .Select(a => new StaffRow(
                a,
                _context.Orders.Count(o => o.AcceptedByAccountId == a.Id)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var account = await _context.Accounts
            .SingleOrDefaultAsync(a => a.Id == id, cancellationToken);

        if (account is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
            return RedirectToPage();
        }

        if (account.Id == GetCurrentAccountId())
        {
            TempData["ErrorMessage"] = "Không thể tự khoá tài khoản của chính mình.";
            return RedirectToPage();
        }

        // Khoá nốt chủ quán cuối cùng = không còn ai tạo lại tài khoản được nữa.
        if (account.IsActive && account.Role == AccountRole.Manager)
        {
            var otherActiveAdmins = await _context.Accounts.CountAsync(
                a => a.Role == AccountRole.Manager && a.IsActive && a.Id != account.Id,
                cancellationToken);

            if (otherActiveAdmins == 0)
            {
                TempData["ErrorMessage"] =
                    "Phải còn ít nhất một tài khoản chủ quán đang hoạt động.";
                return RedirectToPage();
            }
        }

        account.IsActive = !account.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = account.IsActive
            ? $"Đã mở khoá tài khoản {account.Username}."
            : $"Đã khoá tài khoản {account.Username}.";

        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateHourlyRateAsync(
        int id,
        decimal hourlyRate,
        CancellationToken cancellationToken = default)
    {
        if (hourlyRate < 0 || hourlyRate > 1_000_000m)
        {
            TempData["ErrorMessage"] = "Đơn giá phải từ 0 đến 1.000.000đ/giờ.";
            return RedirectToPage();
        }

        var account = await _context.Accounts
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (account is null)
        {
            TempData["ErrorMessage"] = "Không tìm thấy tài khoản.";
            return RedirectToPage();
        }

        account.HourlyRate = hourlyRate;
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] =
            $"Đã cập nhật đơn giá của {account.FullName ?? account.Username}.";
        return RedirectToPage();
    }

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
