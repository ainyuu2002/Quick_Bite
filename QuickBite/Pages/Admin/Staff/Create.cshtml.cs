using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuickBite.Data;
using QuickBite.Models;
using System.ComponentModel.DataAnnotations;

namespace QuickBite.Pages.Admin.Staff;

public class CreateModel : PageModel
{
    private readonly AppDbContext _context;

    public CreateModel(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Model riêng cho form, KHÔNG bind thẳng vào entity <see cref="Account"/>.
    /// Lý do: entity có <c>PasswordHash</c> và <c>IsActive</c> — bind thẳng thì kẻ xấu
    /// có thể thêm field vào form để tự đặt hash hoặc bật quyền (over-posting).
    /// Form chỉ nhận đúng những gì nó cần.
    /// </summary>
    public sealed class InputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập")]
        [StringLength(50, MinimumLength = 3,
            ErrorMessage = "Tên đăng nhập từ 3 đến 50 ký tự")]
        [RegularExpression("^[a-zA-Z0-9_.]+$",
            ErrorMessage = "Tên đăng nhập chỉ gồm chữ, số, dấu chấm và gạch dưới")]
        public string Username { get; set; } = string.Empty;

        [StringLength(100)]
        public string? FullName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Mật khẩu tối thiểu 6 ký tự")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập lại mật khẩu")]
        [DataType(DataType.Password)]
        [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại không khớp")]
        public string ConfirmPassword { get; set; } = string.Empty;

        public AccountRole Role { get; set; } = AccountRole.Staff;

        [Range(0, 1_000_000, ErrorMessage = "Đơn giá phải từ 0 đến 1.000.000đ/giờ.")]
        public decimal HourlyRate { get; set; } = 25_000m;
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var username = Input.Username.Trim();

        // Cột Username có unique index — không kiểm tra trước thì người dùng nhận
        // DbUpdateException khó hiểu thay vì thông báo lỗi tử tế trên form.
        var taken = await _context.Accounts
            .AnyAsync(a => a.Username == username, cancellationToken);

        if (taken)
        {
            ModelState.AddModelError("Input.Username", "Tên đăng nhập này đã tồn tại.");
            return Page();
        }

        var account = new Account
        {
            Username = username,
            FullName = string.IsNullOrWhiteSpace(Input.FullName) ? null : Input.FullName.Trim(),
            Role = Enum.IsDefined(Input.Role) ? Input.Role : AccountRole.Staff,
            HourlyRate = Input.HourlyRate,
            IsActive = true
        };

        // Cùng thuật toán với lúc đăng nhập (PasswordHasher<Account>) — đổi kiểu generic
        // ở một trong hai nơi là verify sẽ luôn thất bại.
        account.PasswordHash = new PasswordHasher<Account>()
            .HashPassword(account, Input.Password);

        _context.Accounts.Add(account);
        await _context.SaveChangesAsync(cancellationToken);

        TempData["SuccessMessage"] =
            $"Đã tạo tài khoản {account.Username} ({account.Role.ToDisplayText()}).";

        return RedirectToPage("./Index");
    }
}
