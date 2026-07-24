using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

/// <summary>
/// Tài khoản đăng nhập khu quản trị — gồm cả chủ quán và nhân viên.
/// <para>
/// Quyết định thiết kế: KHÔNG tách bảng Staff riêng. Quán ăn đơn lẻ nên mọi tài khoản
/// đều là người làm việc tại quán; tách 1-1 chỉ thêm join mà không chuẩn hoá được gì.
/// Phân biệt quyền hạn bằng <see cref="Role"/>. (Xem 03-SDS.)
/// </para>
/// </summary>
public class Account
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Username { get; set; } = null!;

    /// <summary>Hash bằng PasswordHasher&lt;Account&gt; — KHÔNG lưu plain text (NFR-03).</summary>
    [Required]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    public string? FullName { get; set; }

    public AccountRole Role { get; set; } = AccountRole.Staff;

    /// <summary>
    /// Tắt để khoá đăng nhập mà vẫn giữ nguyên lịch sử đơn đã nhận và ca làm đã chấm công
    /// — cùng lý do với MenuItem.IsAvailable: không xoá cứng dữ liệu đã được tham chiếu.
    /// </summary>
    public bool IsActive { get; set; } = true;

    public List<WorkSession> WorkSessions { get; set; } = new();
}
