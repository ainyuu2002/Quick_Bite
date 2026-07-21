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
    private readonly AppDbContext _context;

    public IndexModel(AppDbContext context)
    {
        _context = context;
    }

    public List<Category> Categories { get; private set; } = new();

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
            return RedirectToPage();
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

        return RedirectToPage();
    }

    private async Task LoadMenuAsync()
    {
        Categories = await _context.Categories
            .AsNoTracking()
            .Include(category => category.MenuItems.OrderBy(item => item.Name))
            .OrderBy(category => category.DisplayOrder)
            .ThenBy(category => category.Name)
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
