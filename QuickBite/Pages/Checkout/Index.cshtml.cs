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
    private readonly CustomerAccountService _accounts;
    private readonly LoyaltyService _loyalty;

    public IndexModel(
        OrderService orderService,
        CustomerAccountService accounts,
        LoyaltyService loyalty)
    {
        _orderService = orderService;
        _accounts = accounts;
        _loyalty = loyalty;
    }

    [BindProperty]
    public CheckoutInput Input { get; set; } = new();

    public IReadOnlyList<CartItem> Cart { get; private set; } = [];

    public decimal Total => Cart.Sum(item => item.Subtotal);

    public int TotalQuantity => Cart.Sum(item => item.Quantity);

    public bool IsMember { get; private set; }

    public IReadOnlyList<Voucher> AvailableVouchers { get; private set; } = [];

    public int PotentialPoints => LoyaltyService.PointsFor(Total);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        LoadCart();

        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        if (customerId is null)
        {
            return;
        }

        var customer = await _accounts.GetAsync(customerId.Value, cancellationToken);
        if (customer is null)
        {
            return;
        }

        IsMember = true;
        Input.CustomerName = customer.FullName;
        Input.Phone = customer.Phone;
        if (!string.IsNullOrWhiteSpace(customer.SavedAddress))
        {
            Input.Address = customer.SavedAddress;
        }

        await LoadAvailableVouchersAsync(customerId.Value, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        LoadCart();

        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        IsMember = customerId is not null;
        if (customerId is not null)
        {
            await LoadAvailableVouchersAsync(customerId.Value, cancellationToken);
        }

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
                    Cart.Select(item => new CreateOrderItem(item.MenuItemId, item.Quantity)).ToArray(),
                    customerId,
                    Input.PromotionCode,
                    Input.VoucherCode),
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

    private async Task LoadAvailableVouchersAsync(
        int customerId,
        CancellationToken cancellationToken)
    {
        var vouchers = await _loyalty.GetVouchersAsync(customerId, cancellationToken);
        AvailableVouchers = vouchers
            .Where(v => v.UsedAt is null && v.ExpiresAt >= DateTime.Now)
            .OrderBy(v => v.ExpiresAt)
            .ToList();
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

        [StringLength(30, ErrorMessage = "Mã khuyến mãi không hợp lệ.")]
        [Display(Name = "Mã khuyến mãi")]
        public string? PromotionCode { get; set; }

        [StringLength(30, ErrorMessage = "Mã voucher không hợp lệ.")]
        [Display(Name = "Voucher của bạn")]
        public string? VoucherCode { get; set; }
    }
}
