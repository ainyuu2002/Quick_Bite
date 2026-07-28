using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public enum PointEntryType
{
    Earn = 0,
    Redeem = 1,
    Adjust = 2
}

public class PointLedger
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int Points { get; set; }

    public PointEntryType Type { get; set; }

    public int? OrderId { get; set; }

    public int? VoucherId { get; set; }

    [StringLength(200)]
    public string? Note { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public static class PointEntryTypeExtensions
{
    public static string ToDisplayText(this PointEntryType type) => type switch
    {
        PointEntryType.Earn => "Tích điểm",
        PointEntryType.Redeem => "Đổi voucher",
        PointEntryType.Adjust => "Điều chỉnh",
        _ => type.ToString()
    };
}
