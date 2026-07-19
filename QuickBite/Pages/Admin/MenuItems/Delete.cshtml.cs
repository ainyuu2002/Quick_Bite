using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Data;

namespace QuickBite.Pages.Admin.MenuItems;

public class DeleteModel : PageModel
{
    private readonly AppDbContext _context;

    public DeleteModel(AppDbContext context)
    {
        _context = context;
    }

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
        else
        {
            MenuItem = menuitem;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var menuitem = await _context.MenuItems.FindAsync(id);
        if (menuitem is null)
        {
            return RedirectToPage("./Index");
        }

        _context.MenuItems.Remove(menuitem);

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            TempData["ErrorMessage"] =
                $"Không thể xoá \"{menuitem.Name}\" vì món này đã có trong đơn hàng. " +
                "Hãy dùng nút ẩn ở trang danh sách để ngừng bán thay vì xoá.";
        }

        return RedirectToPage("./Index");
    }
}
