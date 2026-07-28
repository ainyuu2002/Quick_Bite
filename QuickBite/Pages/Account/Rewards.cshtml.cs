using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.CustomerAccount;

public sealed class RewardsModel : PageModel
{
    private readonly LoyaltyService _loyalty;

    public RewardsModel(LoyaltyService loyalty)
    {
        _loyalty = loyalty;
    }

    public int PointBalance { get; private set; }

    public IReadOnlyList<Voucher> Vouchers { get; private set; } = [];

    public IReadOnlyList<PointLedger> Ledger { get; private set; } = [];

    public IReadOnlyList<RedeemOption> Options => LoyaltyService.RedeemOptions;

    [TempData]
    public string? Message { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        if (customerId is null)
        {
            return RedirectToPage("/Account/Login");
        }

        await LoadAsync(customerId.Value, cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostRedeemAsync(
        string option,
        CancellationToken cancellationToken)
    {
        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        if (customerId is null)
        {
            return RedirectToPage("/Account/Login");
        }

        try
        {
            var voucher = await _loyalty.RedeemAsync(customerId.Value, option, cancellationToken);
            Message = $"Đổi thành công! Voucher {voucher.Code} giảm {voucher.DiscountPercent}% " +
                $"(tối đa {voucher.MaxDiscountAmount:N0} đ), hạn dùng đến {voucher.ExpiresAt:dd/MM/yyyy}.";
        }
        catch (CustomerFlowException exception)
        {
            ErrorMessage = exception.Message;
        }

        return RedirectToPage();
    }

    private async Task LoadAsync(int customerId, CancellationToken cancellationToken)
    {
        PointBalance = await _loyalty.GetBalanceAsync(customerId, cancellationToken);
        Vouchers = await _loyalty.GetVouchersAsync(customerId, cancellationToken);
        Ledger = await _loyalty.GetLedgerAsync(customerId, cancellationToken);
    }
}
