using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Data;

namespace QuickBite.Pages.Admin.MenuItems;

public class IndexModel : PageModel
{
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public IList<MenuItem> MenuItem { get; set; } = default!;

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        MenuItem = await _context.MenuItems
            .Include(m => m.Category)
            .OrderBy(m => m.Name)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostToggleAvailableAsync(int id)
    {
        var menuItem = await _context.MenuItems.FindAsync(id);
        if (menuItem is null)
        {
            return NotFound();
        }

        menuItem.IsAvailable = !menuItem.IsAvailable;
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }
}
