using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Admin.Promotions;

public class CreateModel : PageModel
{
    private readonly AppDbContext _db;

    public CreateModel(AppDbContext db)
    {
        _db = db;
    }

    [BindProperty]
    public Promotion Promotion { get; set; } = default!;

    public void OnGet()
    {
        Promotion = new Promotion();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        PromotionRules.Validate(Promotion, ModelState);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        var codeTaken = await _db.Promotions
            .AnyAsync(p => p.Code == Promotion.Code, cancellationToken);
        if (codeTaken)
        {
            ModelState.AddModelError("Promotion.Code", "Mã này đã tồn tại.");
            return Page();
        }

        Promotion.CreatedAt = DateTime.Now;
        _db.Promotions.Add(Promotion);
        await _db.SaveChangesAsync(cancellationToken);
        return RedirectToPage("./Index");
    }
}
