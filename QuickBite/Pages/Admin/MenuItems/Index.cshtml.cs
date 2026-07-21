using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Models;
using QuickBite.Data;

namespace QuickBite.Pages.Admin.MenuItems;

public class IndexModel : PageModel
{
    private const int PageSize = 10;

    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public PagedResult<MenuItem> Result { get; set; } = default!;

    public IList<Category> Categories { get; set; } = default!;

    public string? Search { get; private set; }

    public int? CategoryId { get; private set; }

    public string Available { get; private set; } = "all";

    public string SortBy { get; private set; } = "name";

    public string SortDir { get; private set; } = "asc";

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync(
        string? search = null,
        int? categoryId = null,
        string available = "all",
        string sortBy = "name",
        string sortDir = "asc",
        int page = 1)
    {
        Search = search;
        CategoryId = categoryId;
        Available = available is "available" or "unavailable" ? available : "all";
        SortBy = sortBy == "price" ? "price" : "name";
        SortDir = sortDir == "desc" ? "desc" : "asc";

        Categories = await _context.Categories
            .OrderBy(c => c.DisplayOrder)
            .ToListAsync();

        var query = _context.MenuItems.Include(m => m.Category).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim();
            query = query.Where(m => m.Name.Contains(keyword));
        }

        if (categoryId is not null)
        {
            query = query.Where(m => m.CategoryId == categoryId);
        }

        query = Available switch
        {
            "available" => query.Where(m => m.IsAvailable),
            "unavailable" => query.Where(m => !m.IsAvailable),
            _ => query
        };

        query = (SortBy, SortDir) switch
        {
            ("price", "asc") => query.OrderBy(m => m.Price),
            ("price", "desc") => query.OrderByDescending(m => m.Price),
            ("name", "desc") => query.OrderByDescending(m => m.Name),
            _ => query.OrderBy(m => m.Name)
        };

        Result = await PagedResult<MenuItem>.CreateAsync(query, page, PageSize);
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