using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.Orders;

public sealed class TrackModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly OrderService _orderService;
    private readonly CustomerAccountService _accounts;

    public TrackModel(
        AppDbContext db,
        OrderService orderService,
        CustomerAccountService accounts)
    {
        _db = db;
        _orderService = orderService;
        _accounts = accounts;
    }

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại đã dùng để đặt hàng.")]
    [RegularExpression(
        @"^0\d{8,10}$",
        ErrorMessage = "Số điện thoại phải có 9–11 chữ số và bắt đầu bằng 0.")]
    public string Phone { get; set; } = string.Empty;

    public IReadOnlyList<Order> Orders { get; private set; } = [];

    public bool Created { get; private set; }

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
        await LoadAuthorizedOrderAsync(cancellationToken);

        if (Created && Orders.Count > 0)
        {
            var phone = Orders[0].Phone;
            var isMember = await CustomerAuth.GetCustomerIdAsync(HttpContext) is not null
                || await _accounts.IsPhoneRegisteredAsync(phone, cancellationToken);
            if (!isMember)
            {
                SuggestRegistration = true;
                RegistrationPhone = phone;
                PotentialPoints = LoyaltyService.PointsFor(Orders[0].Total);
            }
        }
    }

    public async Task<IActionResult> OnPostLookupAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var normalizedPhone = Phone.Trim();
        if (!await _orderService.HasOrdersByPhoneAsync(normalizedPhone, cancellationToken))
        {
            ModelState.AddModelError(string.Empty, "Không tìm thấy đơn hàng phù hợp.");
            return Page();
        }

        OrderTrackingSession.GrantAccess(HttpContext.Session, normalizedPhone);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostCancelAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        var trackedPhone = OrderTrackingSession.GetTrackedPhone(HttpContext.Session);
        if (string.IsNullOrWhiteSpace(trackedPhone))
        {
            ErrorMessage = "Phiên tra cứu đã hết hạn. Vui lòng nhập lại số điện thoại.";
            return RedirectToPage();
        }

        try
        {
            await _orderService.CancelOrderAsync(
                orderId,
                trackedPhone,
                cancellationToken: cancellationToken);
            Message = "Đơn hàng đã được hủy thành công.";
        }
        catch (Exception exception) when (exception is OrderValidationException or KeyNotFoundException)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage();
    }

    public IActionResult OnPostClear()
    {
        OrderTrackingSession.ClearAccess(HttpContext.Session);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnGetStatusAsync(CancellationToken cancellationToken)
    {
        var trackedPhone = OrderTrackingSession.GetTrackedPhone(HttpContext.Session);
        if (string.IsNullOrWhiteSpace(trackedPhone))
        {
            return NotFound();
        }

        var statuses = await _db.Orders
            .AsNoTracking()
            .Where(order => order.Phone == trackedPhone)
            .OrderByDescending(order => order.CreatedAt)
            .ThenByDescending(order => order.Id)
            .Select(order => new
            {
                order.Id,
                order.Status
            })
            .ToListAsync(cancellationToken);

        if (statuses.Count == 0)
        {
            return NotFound();
        }

        return new JsonResult(new
        {
            orders = statuses.Select(status => new
            {
                id = status.Id,
                status = (int)status.Status,
                statusText = status.Status.ToDisplayText()
            })
        });
    }

    public string MaskedPhone => Orders.Count == 0 ? string.Empty : MaskPhone(Orders[0].Phone);

    private async Task LoadAuthorizedOrderAsync(CancellationToken cancellationToken)
    {
        var trackedPhone = OrderTrackingSession.GetTrackedPhone(HttpContext.Session);
        if (!string.IsNullOrWhiteSpace(trackedPhone))
        {
            Orders = await _orderService.GetOrdersByPhoneAsync(trackedPhone, cancellationToken);
        }
    }

    private static string MaskPhone(string phone)
    {
        if (phone.Length < 7)
        {
            return phone;
        }

        return $"{phone[..3]}****{phone[^3..]}";
    }
}
