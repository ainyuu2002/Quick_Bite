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

    /// <summary>Một dòng trong bảng nhân viên — kèm số đơn người đó đã nhận.</summary>
    public sealed record StaffRow(Account Account, int AcceptedOrderCount);

    public IReadOnlyList<StaffRow> Rows { get; private set; } = Array.Empty<StaffRow>();

    public int CurrentAccountId { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
    {
        CurrentAccountId = GetCurrentAccountId();

        // Đếm đơn ngay trong truy vấn để không bị N+1 (mỗi dòng một lần gọi DB).
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

        // Tự khoá mình = tự đá mình ra khỏi hệ thống ngay lần chuyển trang kế tiếp.
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

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
