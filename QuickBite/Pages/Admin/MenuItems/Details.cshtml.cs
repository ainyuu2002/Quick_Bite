using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Data;

namespace QuickBite.Pages.Admin.MenuItems;

public class DetailsModel : PageModel
{
    private readonly AppDbContext _context;
    public DetailsModel(AppDbContext context)
    {
        _context = context;
    }

    public MenuItem MenuItem { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var menuitem = await _context.MenuItems.FirstOrDefaultAsync(m => m.Id == id);
        if (menuitem is null)
        {
            return NotFound();
        }
        else
        {
            MenuItem = menuitem;
        }

        return Page();
    }
}
