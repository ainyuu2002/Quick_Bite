using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuickBite.Pages.Admin.MenuItems;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    public SelectList CategoryList { get; set; } = default!;

    [BindProperty]
    public MenuItem MenuItem { get; set; } = default!;

    public async Task OnGetAsync()
    {
        CategoryList = new SelectList(
            await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(),
            "Id", "Name");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            CategoryList = new SelectList(
                await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(),
                "Id", "Name");
            return Page();
        }

        _context.MenuItems.Add(MenuItem);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
