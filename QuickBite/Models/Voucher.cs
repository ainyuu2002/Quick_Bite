using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Models;

public class Voucher
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string Code { get; set; } = null!;

    public int CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int DiscountPercent { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal MaxDiscountAmount { get; set; }

    public int PointsSpent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public int? UsedOrderId { get; set; }
}
