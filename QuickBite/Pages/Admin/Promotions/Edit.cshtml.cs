using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Promotions;

public class EditModel : PageModel
{
    private readonly AppDbContext _db;

    public EditModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Promotion Promotion { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int id, CancellationToken cancellationToken)
    {
        var promotion = await _db.Promotions
            .AsNoTracking()
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (promotion is null)
        {
            return RedirectToPage("./Index");
        }

        Promotion = promotion;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id, CancellationToken cancellationToken)
    {
        PromotionRules.Validate(Promotion, ModelState);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var promotion = await _db.Promotions
            .SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (promotion is null)
        {
            return RedirectToPage("./Index");
        }

        var codeTaken = await _db.Promotions
            .AnyAsync(p => p.Code == Promotion.Code && p.Id != id, cancellationToken);
        if (codeTaken)
        {
            ModelState.AddModelError("Promotion.Code", "Mã này đã tồn tại.");
            return Page();
        }

        promotion.Code = Promotion.Code;
        promotion.Description = Promotion.Description;
        promotion.DiscountType = Promotion.DiscountType;
        promotion.DiscountValue = Promotion.DiscountValue;
        promotion.MaxDiscountAmount = Promotion.MaxDiscountAmount;
        promotion.MinOrderTotal = Promotion.MinOrderTotal;
        promotion.StartsAt = Promotion.StartsAt;
        promotion.EndsAt = Promotion.EndsAt;
        promotion.TotalUsageLimit = Promotion.TotalUsageLimit;
        promotion.PerPhoneLimit = Promotion.PerPhoneLimit;
        promotion.IsActive = Promotion.IsActive;
        await _db.SaveChangesAsync(cancellationToken);

        return RedirectToPage("./Index");
    }
}
