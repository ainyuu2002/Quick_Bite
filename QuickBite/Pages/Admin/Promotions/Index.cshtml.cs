using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Promotions;

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;

    public IndexModel(AppDbContext db)
    {
        _db = db;
    }

    public IReadOnlyList<PromotionRow> Promotions { get; private set; } = [];

    public sealed record PromotionRow(Promotion Promotion, int UsedCount);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Promotions = await _db.Promotions
            .AsNoTracking()
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PromotionRow(
                p,
                p.Usages.Count(u => u.RefundedAt == null)))
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostToggleAsync(int id, CancellationToken cancellationToken)
    {
        var promotion = await _db.Promotions
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (promotion is not null)
        {
            promotion.IsActive = !promotion.IsActive;
            await _db.SaveChangesAsync(cancellationToken);
        }

        return RedirectToPage();
    }
}
