using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QuickBite.Models;
using QuickBite.Services;

namespace QuickBite.Pages.CustomerAccount;

public sealed class RegisterModel : PageModel
{
    private static readonly Regex PhonePattern = new(@"^0\d{8,10}$", RegexOptions.Compiled);
    private readonly CustomerAccountService _accounts;
    private readonly OtpService _otp;

    public RegisterModel(CustomerAccountService accounts, OtpService otp)
    {
        _accounts = accounts;
        _otp = otp;
    }

    [BindProperty]
    public string Phone { get; set; } = string.Empty;

    [BindProperty]
    public string? Code { get; set; }

    [BindProperty]
    public string? FullName { get; set; }

    [BindProperty]
    public string? Password { get; set; }

    [BindProperty]
    public string? ConfirmPassword { get; set; }

    public bool ShowVerification { get; private set; }

    public string? IssuedCode { get; private set; }

    public void OnGet(string? phone)
    {
        Phone = phone?.Trim() ?? string.Empty;
    }

    public async Task<IActionResult> OnPostSendCodeAsync(CancellationToken cancellationToken)
    {
        if (!ValidatePhone())
        {
            return Page();
        }

        if (await _accounts.IsPhoneRegisteredAsync(Phone, cancellationToken))
        {
            ModelState.AddModelError(string.Empty,
                "Số điện thoại này đã có tài khoản. Vui lòng đăng nhập.");
            return Page();
        }

        try
        {
            var otp = await _otp.IssueAsync(Phone, OtpPurpose.Register, cancellationToken);
            ShowVerification = true;
            IssuedCode = otp.Code;
        }
        catch (CustomerFlowException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            ShowVerification = true;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostRegisterAsync(CancellationToken cancellationToken)
    {
        var isValid = ValidatePhone();

        if (string.IsNullOrWhiteSpace(Code) || Code.Trim().Length != 6)
        {
            ModelState.AddModelError(nameof(Code), "Vui lòng nhập mã xác thực 6 chữ số.");
            isValid = false;
        }

        if (string.IsNullOrWhiteSpace(FullName) || FullName.Trim().Length > 100)
        {
            ModelState.AddModelError(nameof(FullName), "Vui lòng nhập họ tên (tối đa 100 ký tự).");
            isValid = false;
        }

        if (string.IsNullOrEmpty(Password) || Password.Length < 6)
        {
            ModelState.AddModelError(nameof(Password), "Mật khẩu phải có ít nhất 6 ký tự.");
            isValid = false;
        }
        else if (Password != ConfirmPassword)
        {
            ModelState.AddModelError(nameof(ConfirmPassword), "Mật khẩu nhập lại không khớp.");
            isValid = false;
        }

        if (!isValid)
        {
            ShowVerification = true;
            return Page();
        }

        try
        {
            await _otp.VerifyAsync(Phone, OtpPurpose.Register, Code!, cancellationToken);
            var customer = await _accounts.RegisterAsync(
                Phone, FullName!, Password!, cancellationToken);
            await SignInCustomerAsync(customer);
            return RedirectToPage("/Account/Index");
        }
        catch (CustomerFlowException exception)
        {
            ModelState.AddModelError(string.Empty, exception.Message);
            ShowVerification = true;
            return Page();
        }
    }

    private bool ValidatePhone()
    {
        Phone = Phone.Trim();
        if (PhonePattern.IsMatch(Phone))
        {
            return true;
        }

        ModelState.AddModelError(nameof(Phone),
            "Số điện thoại phải có 9–11 chữ số và bắt đầu bằng 0.");
        return false;
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
