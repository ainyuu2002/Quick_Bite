using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Modules.Operations.Authorization;
using QuickBite.Modules.Operations.Store;

namespace QuickBite.Pages.Admin.Operations;

public sealed class StoreModel : PageModel
{
    private readonly IStoreAvailabilityService _storeAvailability;

    public StoreModel(IStoreAvailabilityService storeAvailability)
        => _storeAvailability = storeAvailability;

    public StoreAvailabilitySnapshot Status { get; private set; } = null!;

    public bool CanManageBusinessHours => User.IsInRole(InternalRoles.Manager);

    [BindProperty]
    public BusinessHoursInput BusinessHours { get; set; } = new();

    [BindProperty]
    public PauseInput Pause { get; set; } = new();

    public sealed class BusinessHoursInput
    {
        [Required(ErrorMessage = "Vui lòng chọn giờ mở cửa.")]
        [DataType(DataType.Time)]
        public TimeOnly? OpensAt { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn giờ đóng cửa.")]
        [DataType(DataType.Time)]
        public TimeOnly? ClosesAt { get; set; }
    }

    public sealed class PauseInput
    {
        [Required(ErrorMessage = "Vui lòng nhập lý do tạm ngưng.")]
        [StringLength(200, ErrorMessage = "Lý do tối đa 200 ký tự.")]
        public string Reason { get; set; } = string.Empty;
    }

    public async Task OnGetAsync(CancellationToken cancellationToken = default)
        => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnPostUpdateHoursAsync(
        CancellationToken cancellationToken = default)
    {
        if (!CanManageBusinessHours)
        {
            return Forbid();
        }

        ModelState.Remove($"{nameof(Pause)}.{nameof(PauseInput.Reason)}");
        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken, preserveHoursInput: true);
            return Page();
        }

        await _storeAvailability.UpdateBusinessHoursAsync(
            BusinessHours.OpensAt!.Value,
            BusinessHours.ClosesAt!.Value,
            GetCurrentAccountId(),
            cancellationToken);

        TempData["SuccessMessage"] = "Đã cập nhật giờ mở cửa.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPauseAsync(
        CancellationToken cancellationToken = default)
    {
        ModelState.Remove(
            $"{nameof(BusinessHours)}.{nameof(BusinessHoursInput.OpensAt)}");
        ModelState.Remove(
            $"{nameof(BusinessHours)}.{nameof(BusinessHoursInput.ClosesAt)}");
        if (string.IsNullOrWhiteSpace(Pause.Reason))
        {
            ModelState.AddModelError(
                $"{nameof(Pause)}.{nameof(PauseInput.Reason)}",
                "Vui lòng nhập lý do tạm ngưng.");
        }

        if (!ModelState.IsValid)
        {
            await LoadAsync(cancellationToken);
            return Page();
        }

        await _storeAvailability.SetPausedAsync(
            true,
            Pause.Reason,
            GetCurrentAccountId(),
            cancellationToken);

        TempData["SuccessMessage"] = "Quán đã tạm ngưng nhận đơn.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostResumeAsync(
        CancellationToken cancellationToken = default)
    {
        await _storeAvailability.SetPausedAsync(
            false,
            null,
            GetCurrentAccountId(),
            cancellationToken);

        TempData["SuccessMessage"] = "Quán đã mở lại nhận đơn.";
        return RedirectToPage();
    }

    private async Task LoadAsync(
        CancellationToken cancellationToken,
        bool preserveHoursInput = false)
    {
        Status = await _storeAvailability.GetStatusAsync(
            cancellationToken: cancellationToken);

        if (!preserveHoursInput)
        {
            BusinessHours = new BusinessHoursInput
            {
                OpensAt = Status.OpensAt,
                ClosesAt = Status.ClosesAt
            };
        }
    }

    private int GetCurrentAccountId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new InvalidOperationException("Không xác định được tài khoản hiện tại.");
}
