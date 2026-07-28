using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Blacklist;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IReadOnlyList<PhoneBlacklist> Entries { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Entries = await _context.PhoneBlacklists
            .AsNoTracking()
            .OrderByDescending(entry => entry.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAddAsync(
        string phone,
        string? reason,
        CancellationToken cancellationToken)
    {
        var normalized = phone?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            TempData["ErrorMessage"] = "Vui lòng nhập số điện thoại.";
            return RedirectToPage();
        }

        var exists = await _context.PhoneBlacklists
            .AnyAsync(entry => entry.Phone == normalized, cancellationToken);
        if (exists)
        {
            TempData["ErrorMessage"] = "Số điện thoại đã có trong danh sách.";
            return RedirectToPage();
        }

        _context.PhoneBlacklists.Add(new PhoneBlacklist
        {
            Phone = normalized,
            Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            CreatedAt = DateTime.Now
        });
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] = $"Đã thêm {normalized} vào danh sách cảnh báo.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync(int id, CancellationToken cancellationToken)
    {
        var entry = await _context.PhoneBlacklists.FindAsync([id], cancellationToken);
        if (entry is not null)
        {
            _context.PhoneBlacklists.Remove(entry);
            await _context.SaveChangesAsync(cancellationToken);
            TempData["SuccessMessage"] = "Đã gỡ số điện thoại khỏi danh sách.";
        }

        return RedirectToPage();
    }
}
