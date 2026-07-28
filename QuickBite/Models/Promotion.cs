using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Models;

public enum DiscountType
{
    Percent = 0,
    Amount = 1
}

public class Promotion
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã khuyến mãi")]
    [StringLength(30)]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Mã chỉ gồm chữ in hoa và số, không dấu cách")]
    public string Code { get; set; } = null!;

    [StringLength(200)]
    public string? Description { get; set; }

    public DiscountType DiscountType { get; set; }

    [Range(1, 100_000_000, ErrorMessage = "Giá trị giảm không hợp lệ")]
    [Column(TypeName = "decimal(18,0)")]
    public decimal DiscountValue { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal? MaxDiscountAmount { get; set; }

    [Range(0, 100_000_000)]
    [Column(TypeName = "decimal(18,0)")]
    public decimal MinOrderTotal { get; set; }

    public DateTime StartsAt { get; set; } = DateTime.Today;

    public DateTime EndsAt { get; set; } = DateTime.Today.AddDays(7);

    [Range(1, 1_000_000)]
    public int? TotalUsageLimit { get; set; }

    [Range(1, 100)]
    public int PerPhoneLimit { get; set; } = 1;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public List<PromotionUsage> Usages { get; set; } = new();
}

public static class DiscountTypeExtensions
{
    public static string ToDisplayText(this DiscountType type) => type switch
    {
        DiscountType.Percent => "Giảm %",
        DiscountType.Amount => "Giảm số tiền",
        _ => type.ToString()
    };
}
