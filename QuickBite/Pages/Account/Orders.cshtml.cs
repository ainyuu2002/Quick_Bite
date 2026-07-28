using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.CustomerAccount;

public sealed class OrdersModel : PageModel
{
    private const string CartKey = "Cart";
    private readonly AppDbContext _db;
    private readonly CustomerAccountService _accounts;
    private readonly OrderService _orderService;

    public OrdersModel(
        AppDbContext db,
        CustomerAccountService accounts,
        OrderService orderService)
    {
        _db = db;
        _accounts = accounts;
        _orderService = orderService;
    }

    public IReadOnlyList<Order> Orders { get; private set; } = [];

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var customer = await LoadCustomerAsync(cancellationToken);
        if (customer is null)
        {
            return RedirectToPage("/Account/Login");
        }

        Orders = await _orderService.GetOrdersByPhoneAsync(customer.Phone, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostReorderAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        var customer = await LoadCustomerAsync(cancellationToken);
        if (customer is null)
        {
            return RedirectToPage("/Account/Login");
        }

        var order = await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.MenuItem)
            .SingleOrDefaultAsync(
                o => o.Id == orderId && o.Phone == customer.Phone,
                cancellationToken);
        if (order is null)
        {
            ErrorMessage = "Không tìm thấy đơn hàng.";
            return RedirectToPage();
        }

        var cart = new List<CartItem>();
        var skipped = 0;
        foreach (var item in order.Items)
        {
            if (item.MenuItem is null || !item.MenuItem.IsAvailable)
            {
                skipped++;
                continue;
            }

            cart.Add(new CartItem
            {
                MenuItemId = item.MenuItem.Id,
                Name = item.MenuItem.Name,
                Price = item.MenuItem.Price,
                ImageUrl = item.MenuItem.ImageUrl,
                Quantity = item.Quantity
            });
        }

        if (cart.Count == 0)
        {
            ErrorMessage = "Các món trong đơn này hiện không còn phục vụ.";
            return RedirectToPage();
        }

        HttpContext.Session.SetString(CartKey, JsonSerializer.Serialize(cart));
        Message = skipped > 0
            ? $"Đã thêm lại {cart.Count} món vào giỏ ({skipped} món không còn phục vụ đã được bỏ qua)."
            : $"Đã thêm lại {cart.Count} món vào giỏ.";
        return RedirectToPage("/Cart/Index");
    }

    private async Task<Customer?> LoadCustomerAsync(CancellationToken cancellationToken)
    {
        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        return customerId is null
            ? null
            : await _accounts.GetAsync(customerId.Value, cancellationToken);
    }
}
