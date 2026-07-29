using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class PhoneBlacklist
{
    public int Id { get; set; }

    [Required]
    [StringLength(11)]
    public string Phone { get; set; } = null!;

    [StringLength(300)]
    public string? Reason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}
