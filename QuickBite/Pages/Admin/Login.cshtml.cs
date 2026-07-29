using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using QuickBite.Modules.Operations.Authorization;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace QuickBite.Pages.Admin
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;

        public LoginModel(AppDbContext db)
        {
            _db = db;
        }

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        public string Username { get; set; } = string.Empty;

        [BindProperty]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        public string Password { get; set; } = string.Empty;

        private static string LandingPageForRole(AccountRole role) => role switch
        {
            AccountRole.Kitchen => "/Admin/Kitchen/Index",
            AccountRole.Shipper => "/Admin/Shipper/Index",
            _ => "/Admin/Orders/Index"
        };

        public IActionResult OnGet()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                var roleName = User.FindFirstValue(ClaimTypes.Role);
                var role = Enum.TryParse<AccountRole>(roleName, out var parsed) ? parsed : AccountRole.Staff;
                return RedirectToPage(LandingPageForRole(role));
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }
            var user = await _db.Accounts.FirstOrDefaultAsync(u => u.Username == Username);
            if (user is null)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
                return Page();
            }

            var hasher = new PasswordHasher<Account>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, Password);

            if(verifyResult == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng");
                return Page();
            }

            if (!user.IsActive)
            {
                ModelState.AddModelError(string.Empty,
                    "Tài khoản đã bị khoá. Vui lòng liên hệ chủ quán.");
                return Page();
            }

            var claims = new List<Claim>
            {
                new (ClaimTypes.Name, user.Username),
                new (ClaimTypes.NameIdentifier, user.Id.ToString()),
                new (ClaimTypes.Role, user.Role switch
                {
                    AccountRole.Manager => InternalRoles.Manager,
                    AccountRole.Staff => InternalRoles.Staff,
                    AccountRole.Kitchen => InternalRoles.Kitchen,
                    AccountRole.Shipper => InternalRoles.Shipper,
                    _ => throw new InvalidOperationException("Vai trò tài khoản không hợp lệ.")
                })
            };
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return RedirectToPage(LandingPageForRole(user.Role));
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToPage("/Admin/Login");
        }
    }
}
