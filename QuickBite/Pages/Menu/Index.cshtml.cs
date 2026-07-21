using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;

namespace QuickBite.Pages.Menu;

public class IndexModel : PageModel
{
    private const string CartKey = "Cart";
    private const int PageSize = 12;
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Category> Categories { get; private set; } = new();

    public List<MenuItem> MenuItems { get; private set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? CategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? SearchTerm { get; set; }

    [BindProperty(SupportsGet = true)]
    public int PageNumber { get; set; } = 1;

    public int TotalItems { get; private set; }

    public int TotalPages { get; private set; }

    public int FirstItemNumber => TotalItems == 0 ? 0 : ((PageNumber - 1) * PageSize) + 1;

    public int LastItemNumber => Math.Min(PageNumber * PageSize, TotalItems);

    public string SelectedCategoryName => CategoryId is null
        ? "Tất cả món ăn"
        : Categories.FirstOrDefault(category => category.Id == CategoryId)?.Name ?? "Danh mục";

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task OnGetAsync()
    {
        await LoadMenuAsync();
    }

    public async Task<IActionResult> OnPostAddToCartAsync(int id)
    {
        var menuItem = await _context.MenuItems
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id);

        if (menuItem is null)
        {
            return NotFound();
        }

        if (!menuItem.IsAvailable)
        {
            ErrorMessage = "Món ăn hiện đã hết hàng.";
            return RedirectToPage(new
            {
                categoryId = CategoryId,
                searchTerm = SearchTerm,
                pageNumber = PageNumber
            });
        }

        var cart = GetCart();
        var cartItem = cart.FirstOrDefault(item => item.MenuItemId == menuItem.Id);

        if (cartItem is null)
        {
            cart.Add(new CartItem
            {
                MenuItemId = menuItem.Id,
                Name = menuItem.Name,
                Price = menuItem.Price,
                ImageUrl = menuItem.ImageUrl,
                Quantity = 1
            });
        }
        else
        {
            cartItem.Quantity++;
        }

        SaveCart(cart);
        Message = $"Đã thêm {menuItem.Name} vào giỏ hàng.";

        return RedirectToPage(new
        {
            categoryId = CategoryId,
            searchTerm = SearchTerm,
            pageNumber = PageNumber
        });
    }

    private async Task LoadMenuAsync()
    {
        Categories = await _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
            .ToListAsync();

        var menuQuery = _context.MenuItems
            .AsNoTracking()
            .Include(item => item.Category)
            .AsQueryable();

        if (CategoryId.HasValue)
        {
            menuQuery = menuQuery.Where(item => item.CategoryId == CategoryId.Value);
        }

        if (!string.IsNullOrWhiteSpace(SearchTerm))
        {
            SearchTerm = SearchTerm.Trim();
            var keyword = SearchTerm;

            menuQuery = menuQuery.Where(item =>
                item.Name.Contains(keyword) ||
                (item.Description != null && item.Description.Contains(keyword)));
        }

        TotalItems = await menuQuery.CountAsync();
        TotalPages = (int)Math.Ceiling(TotalItems / (double)PageSize);

        PageNumber = Math.Max(PageNumber, 1);
        if (TotalPages > 0 && PageNumber > TotalPages)
        {
            PageNumber = TotalPages;
        }

        MenuItems = await menuQuery
            .OrderBy(item => item.Category!.DisplayOrder)
            .ThenBy(item => item.Name)
            .ThenBy(item => item.Id)
            .Skip((PageNumber - 1) * PageSize)
            .Take(PageSize)
            .ToListAsync();
    }

    private List<CartItem> GetCart()
    {
        var json = HttpContext.Session.GetString(CartKey);

        if (string.IsNullOrEmpty(json))
        {
            return new List<CartItem>();
        }

        return JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    private void SaveCart(List<CartItem> cart)
    {
        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CartKey, json);
    }
}
