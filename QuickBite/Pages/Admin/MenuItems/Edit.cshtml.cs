using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Data;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace QuickBite.Pages.Admin.MenuItems;

public class EditModel : PageModel
{
    private readonly AppDbContext _context;

    public EditModel(AppDbContext context)
    {
        _context = context;
    }

    public SelectList CategoryList { get; set; } = default!;

    [BindProperty]
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

        MenuItem = menuitem;
        CategoryList = new SelectList(
            await _context.Categories.OrderBy(c => c.DisplayOrder).ToListAsync(),
            "Id", "Name");
        return Page();
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

        var menuItem = await _context.MenuItems.FindAsync(MenuItem.Id);
        if(menuItem is null)
        {
            return NotFound();
        }

        menuItem.Name = MenuItem.Name;
        menuItem.Description = MenuItem.Description;
        menuItem.Price = MenuItem.Price;
        menuItem.ImageUrl = MenuItem.ImageUrl;
        menuItem.CategoryId = MenuItem.CategoryId;

        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
