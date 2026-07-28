using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Modules.Operations.MenuAvailability;

namespace QuickBite.Pages.Admin.Operations;

public sealed class MenuAvailabilityModel : PageModel
{
    private readonly IMenuAvailabilityService _availability;

    public MenuAvailabilityModel(IMenuAvailabilityService availability)
        => _availability = availability;

    public IReadOnlyList<MenuAvailabilitySnapshot> Items { get; private set; }
        = Array.Empty<MenuAvailabilitySnapshot>();

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public sealed class InputModel
    {
        [Range(1, int.MaxValue)]
        public int MenuItemId { get; set; }

        [Range(1, 10_000, ErrorMessage = "Quota phải từ 1 đến 10.000 phần.")]
        public int DailyLimit { get; set; }

        [DataType(DataType.Time)]
        public TimeOnly? SaleStartsAt { get; set; }

        [DataType(DataType.Time)]
        public TimeOnly? SaleEndsAt { get; set; }
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
        => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostSaveAsync(
        CancellationToken cancellationToken = default)
    {
        if ((Input.SaleStartsAt is null) != (Input.SaleEndsAt is null))
        {
            ModelState.AddModelError(
                $"{nameof(Input)}.{nameof(InputModel.SaleEndsAt)}",
                "Hãy nhập cả giờ bắt đầu và kết thúc, hoặc để trống cả hai.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        await _availability.ConfigureAsync(
            Input.MenuItemId,
            Input.DailyLimit,
            Input.SaleStartsAt,
            Input.SaleEndsAt,
            GetCurrentAccountId(),
            cancellationToken);

        TempData["SuccessMessage"] = "Đã cập nhật quota và khung giờ bán.";
        return RedirectToPage();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
        => Items = await _availability.GetStatusesAsync(
            cancellationToken: cancellationToken);

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Không xác định được tài khoản hiện tại.");
}
