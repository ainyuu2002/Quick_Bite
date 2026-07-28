using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.CustomerAccount;

public sealed class IndexModel : PageModel
{
    private readonly CustomerAccountService _accounts;
    private readonly LoyaltyService _loyalty;

    public IndexModel(CustomerAccountService accounts, LoyaltyService loyalty)
    {
        _accounts = accounts;
        _loyalty = loyalty;
    }

    [BindProperty]
    public ProfileInput Input { get; set; } = new();

    public Customer Customer { get; private set; } = null!;

    public CustomerBadge Badge { get; private set; } = new(0, 0);

    public int PointBalance { get; private set; }

    [TempData]
    public string? Message { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var customer = await LoadCustomerAsync(cancellationToken);
        if (customer is null)
        {
            return RedirectToPage("/Account/Login");
        }

        Input.FullName = customer.FullName;
        Input.SavedAddress = customer.SavedAddress;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var customer = await LoadCustomerAsync(cancellationToken);
        if (customer is null)
        {
            return RedirectToPage("/Account/Login");
        }

        if (!ModelState.IsValid)
        {
            return Page();
        }

        await _accounts.UpdateProfileAsync(
            customer.Id, Input.FullName, Input.SavedAddress, cancellationToken);
        Message = "Đã lưu thông tin tài khoản.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostLogoutAsync()
    {
        await HttpContext.SignOutAsync(CustomerAuth.Scheme);
        return RedirectToPage("/Menu/Index");
    }

    private async Task<Customer?> LoadCustomerAsync(CancellationToken cancellationToken)
    {
        var customerId = await CustomerAuth.GetCustomerIdAsync(HttpContext);
        if (customerId is null)
        {
            return null;
        }

        var customer = await _accounts.GetAsync(customerId.Value, cancellationToken);
        if (customer is null)
        {
            return null;
        }

        Customer = customer;
        Badge = await _accounts.GetBadgeAsync(customer.Phone, cancellationToken);
        PointBalance = await _loyalty.GetBalanceAsync(customer.Id, cancellationToken);
        return customer;
    }

    public sealed class ProfileInput
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên không được vượt quá 100 ký tự.")]
        [Display(Name = "Họ và tên")]
        public string FullName { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Địa chỉ không được vượt quá 500 ký tự.")]
        [Display(Name = "Địa chỉ giao hàng mặc định")]
        public string? SavedAddress { get; set; }
    }
}
