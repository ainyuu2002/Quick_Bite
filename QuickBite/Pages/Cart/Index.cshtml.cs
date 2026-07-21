using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;

namespace QuickBite.Pages.Cart;

public class IndexModel : PageModel
{
    private const string CartKey = "Cart";

    public List<CartItem> Cart { get; private set; } = new();

    public int TotalQuantity => Cart.Sum(item => item.Quantity);

    public decimal Total => Cart.Sum(item => item.Subtotal);

    public void OnGet()
    {
        Cart = GetCart();
    }

    public IActionResult OnPostIncrease(int id)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(item => item.MenuItemId == id);

        if (item is not null)
        {
            item.Quantity++;
            SaveCart(cart);
        }

        return RedirectToPage();
    }

    public IActionResult OnPostDecrease(int id)
    {
        var cart = GetCart();
        var item = cart.FirstOrDefault(item => item.MenuItemId == id);

        if (item is not null)
        {
            item.Quantity--;

            if (item.Quantity <= 0)
            {
                cart.Remove(item);
            }

            SaveCart(cart);
        }

        return RedirectToPage();
    }

    public IActionResult OnPostRemove(int id)
    {
        var cart = GetCart();
        cart.RemoveAll(item => item.MenuItemId == id);
        SaveCart(cart);

        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        HttpContext.Session.Remove(CartKey);
        return RedirectToPage();
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
        if (cart.Count == 0)
        {
            HttpContext.Session.Remove(CartKey);
            return;
        }

        var json = JsonSerializer.Serialize(cart);
        HttpContext.Session.SetString(CartKey, json);
    }
}
