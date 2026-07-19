using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class AdminUser
{
    public int Id { get; set; }

    [Required, StringLength(50)]
    public string Username { get; set; } = null!;

    /// <summary>Hash bằng PasswordHasher&lt;AdminUser&gt; — KHÔNG lưu plain text (NFR-03).</summary>
    [Required]
    public string PasswordHash { get; set; } = null!;

    [StringLength(100)]
    public string? FullName { get; set; }
}
