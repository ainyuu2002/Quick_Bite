using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Checkout;

public sealed class IndexModel : PageModel
{
    private const string CartKey = "Cart";
    private readonly OrderService _orderService;

    public IndexModel(OrderService orderService)
    {
        _orderService = orderService;
    }

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    public IReadOnlyList<CartItem> Cart { get; private set; } = [];

    public decimal Total => Cart.Sum(item => item.Subtotal);

    public int TotalQuantity => Cart.Sum(item => item.Quantity);

    public void OnGet()
    {
        LoadCart();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        LoadCart();

        if (Cart.Count == 0)
        {
            ModelState.AddModelError(string.Empty, "Giỏ hàng đang trống. Vui lòng chọn món trước khi thanh toán.");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var order = await _orderService.CreateOrderAsync(
                new CreateOrderRequest(
                    Input.CustomerName,
                    Input.Phone,
                    Input.Address,
                    Input.Note,
                    Input.PaymentMethod!.Value,
                    Cart.Select(item => new CreateOrderItem(item.MenuItemId, item.Quantity)).ToArray()),
                cancellationToken);

            HttpContext.Session.Remove(CartKey);
            OrderTrackingSession.GrantAccess(HttpContext.Session, order.Phone);
            return RedirectToPage("/Orders/Track", new { created = true });
        }
        catch (OrderValidationException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            return Page();
        }
    }

    private void LoadCart()
    {
        var json = HttpContext.Session.GetString(CartKey);
        if (string.IsNullOrWhiteSpace(json))
        {
            Cart = [];
            return;
        }

        try
        {
            Cart = JsonSerializer.Deserialize<List<CartItem>>(json)?
                .Where(item => item.MenuItemId > 0 && item.Quantity > 0)
                .ToList() ?? [];
        }
        catch (JsonException)
        {
            HttpContext.Session.Remove(CartKey);
            Cart = [];
        }
    }

    public sealed class CheckoutInput
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string CustomerName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
        [RegularExpression(
            @"^0\d{8,10}$",
            ErrorMessage = "Số điện thoại phải có 9–11 chữ số và bắt đầu bằng 0.")]
        [Display(Name = "Số điện thoại")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng.")]
        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string Address { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod? PaymentMethod { get; set; } = QuickBite.Models.PaymentMethod.Cash;
    }
}
