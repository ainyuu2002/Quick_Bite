using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.CustomerAccount;

public sealed class LoginModel : PageModel
{
    private readonly CustomerAccountService _accounts;

    public LoginModel(CustomerAccountService accounts)
    {
        _accounts = accounts;
    }

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập số điện thoại.")]
    [RegularExpression(
        @"^0\d{8,10}$",
        ErrorMessage = "Số điện thoại phải có 9–11 chữ số và bắt đầu bằng 0.")]
    public string Phone { get; set; } = string.Empty;

    [BindProperty]
    [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
    public string Password { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync()
    {
        if (await CustomerAuth.GetCustomerIdAsync(HttpContext) is not null)
        {
            return RedirectToPage("/Account/Index");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var customer = await _accounts.ValidateLoginAsync(Phone, Password, cancellationToken);
        if (customer is null)
        {
            ModelState.AddModelError(string.Empty, "Số điện thoại hoặc mật khẩu không đúng.");
            return Page();
        }

        await SignInCustomerAsync(customer);
        return RedirectToPage("/Account/Index");
    }

    private async Task SignInCustomerAsync(Customer customer)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, customer.Id.ToString()),
            new(ClaimTypes.Name, customer.FullName),
            new(ClaimTypes.MobilePhone, customer.Phone),
            new(ClaimTypes.Role, "Customer")
        };
        var identity = new ClaimsIdentity(claims, CustomerAuth.Scheme);
        await HttpContext.SignInAsync(CustomerAuth.Scheme, new ClaimsPrincipal(identity));
    }
}
