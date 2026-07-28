using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Checkout;

public sealed class IndexModel : PageModel
{
    private const string CartKey = "Cart";
    private readonly OrderService _orderService;
    private readonly OrderingOptions _options;

    public IndexModel(OrderService orderService, IOptions<OrderingOptions> options)
    {
        _orderService = orderService;
        _options = options.Value;
    }

    public decimal DeliveryFee => _options.DeliveryFee;

    public decimal MinimumDeliverySubtotal => _options.MinimumDeliverySubtotal;

    public decimal PartyThreshold => _options.PartyThreshold;

    public int PartyDepositPercent => _options.PartyDepositPercent;

    public int PartyMinLeadHours => _options.PartyMinLeadHours;

    public bool IsParty => Total > _options.PartyThreshold;

    public decimal EstimatedDeposit => Math.Round(Total * _options.PartyDepositPercent / 100m, 0);

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

        if (Input.OrderType == OrderType.Delivery && string.IsNullOrWhiteSpace(Input.Address))
        {
            ModelState.AddModelError("Input.Address", "Vui lòng nhập địa chỉ giao hàng.");
        }

        if (IsParty)
        {
            var earliest = DateTime.Now.AddHours(_options.PartyMinLeadHours);
            if (Input.ScheduledFor is null)
            {
                ModelState.AddModelError("Input.ScheduledFor", "Vui lòng chọn thời gian nhận tiệc.");
            }
            else if (Input.ScheduledFor < earliest)
            {
                ModelState.AddModelError("Input.ScheduledFor",
                    $"Phải hẹn trước tối thiểu {_options.PartyMinLeadHours} giờ.");
            }
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
                    Input.OrderType == OrderType.Pickup ? null : Input.Address,
                    Input.Note,
                    Input.OrderType,
                    Input.PaymentMethod!.Value,
                    IsParty,
                    Input.ScheduledFor,
                    Cart.Select(item => new CreateOrderItem(item.MenuItemId, item.Quantity)).ToArray()),
                cancellationToken);

            HttpContext.Session.Remove(CartKey);
            return RedirectToPage("/Orders/Track", new { code = order.OrderCode, created = true });
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

        [Display(Name = "Hình thức nhận hàng")]
        public OrderType OrderType { get; set; } = OrderType.Delivery;

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string? Address { get; set; }

        [StringLength(500, ErrorMessage = "Ghi chú không được vượt quá 500 ký tự.")]
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }

        [Display(Name = "Thời gian nhận tiệc")]
        public DateTime? ScheduledFor { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        [Display(Name = "Phương thức thanh toán")]
        public PaymentMethod? PaymentMethod { get; set; } = QuickBite.Models.PaymentMethod.Cash;
    }
}
