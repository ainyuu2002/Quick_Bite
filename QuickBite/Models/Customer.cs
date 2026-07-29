using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class Customer
{
    public int Id { get; set; }

    [Required, StringLength(11)]
    public string Phone { get; set; } = null!;

    [Required]
    public string PasswordHash { get; set; } = null!;

    [Required, StringLength(100)]
    public string FullName { get; set; } = null!;

    [StringLength(500)]
    public string? SavedAddress { get; set; }

    public bool IsPhoneVerified { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<Voucher> Vouchers { get; set; } = new();

    public List<PointLedger> PointEntries { get; set; } = new();
}
