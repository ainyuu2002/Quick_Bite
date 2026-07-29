using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Orders;

public sealed class TrackModel : PageModel
{
    private readonly OrderService _orderService;
    private readonly CustomerAccountService _accounts;

    public TrackModel(
        OrderService orderService,
        CustomerAccountService accounts)
    {
        _orderService = orderService;
        _accounts = accounts;
    }

    [BindProperty(SupportsGet = true)]
    [Display(Name = "Mã tra cứu")]
    public string? Code { get; set; }

    public Order? Order { get; private set; }

    public string? StatusReason { get; private set; }

    public IReadOnlyList<ReasonCatalog> CancelReasons { get; private set; } = [];

    public bool Created { get; private set; }

    public bool Searched { get; private set; }

    public bool SuggestRegistration { get; private set; }

    public int PotentialPoints { get; private set; }

    public string RegistrationPhone { get; private set; } = string.Empty;

    [TempData]
    public string? ErrorMessage { get; set; }

    [TempData]
    public string? Message { get; set; }

    public async Task OnGetAsync(bool created = false, CancellationToken cancellationToken = default)
    {
        Created = created;

        if (string.IsNullOrWhiteSpace(Code))
        {
            return;
        }

        Searched = true;
        Order = await _orderService.GetByCodeAsync(Code, cancellationToken);

        if (Created && Order is not null)
        {
            var isSignedIn = await CustomerAuth.GetCustomerIdAsync(HttpContext) is not null;
            var isRegistered = await _accounts.IsPhoneRegisteredAsync(
                Order.Phone,
                cancellationToken);

            if (!isSignedIn && !isRegistered)
            {
                SuggestRegistration = true;
                RegistrationPhone = Order.Phone;
                PotentialPoints = LoyaltyService.PointsFor(Order.Total);
            }
        }

        if (Order is { Status: OrderStatus.Pending })
        {
            CancelReasons = await _orderService.GetReasonsAsync(ReasonKind.Cancel, cancellationToken);
        }

        if (Order is not null && Order.Status is OrderStatus.Cancelled or OrderStatus.Rejected
            or OrderStatus.Expired or OrderStatus.DeliveryFailed or OrderStatus.NoShow)
        {
            StatusReason = await _orderService.GetLatestReasonAsync(Order.Id, cancellationToken);
        }
    }

    public IActionResult OnPostLookup()
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ModelState.AddModelError(nameof(Code), "Vui lòng nhập mã tra cứu.");
            return Page();
        }

        return RedirectToPage(new { code = Code.Trim().ToUpperInvariant() });
    }

    public async Task<IActionResult> OnPostCancelAsync(
        string? reason,
        string? reasonOther,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(Code))
        {
            ErrorMessage = "Thiếu mã tra cứu.";
            return RedirectToPage();
        }

        var effectiveReason = reason == "__other__" ? reasonOther : reason;

        try
        {
            await _orderService.CancelByCodeAsync(Code, effectiveReason, cancellationToken);
            Message = "Đơn hàng đã được hủy.";
        }
        catch (Exception exception) when (exception is OrderValidationException or KeyNotFoundException)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage(new { code = Code });
    }
}
