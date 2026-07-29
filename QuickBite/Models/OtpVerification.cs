using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public enum OtpPurpose
{
    Register = 0
}

public class OtpVerification
{
    public int Id { get; set; }

    [Required, StringLength(11)]
    public string Phone { get; set; } = null!;

    [Required, StringLength(6)]
    public string Code { get; set; } = null!;

    public OtpPurpose Purpose { get; set; } = OtpPurpose.Register;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public int FailedAttempts { get; set; }
}
