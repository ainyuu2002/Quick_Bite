using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class PromotionUsage
{
    public int Id { get; set; }

    public int PromotionId { get; set; }

    public Promotion? Promotion { get; set; }

    [Required, StringLength(11)]
    public string Phone { get; set; } = null!;

    public int OrderId { get; set; }

    public DateTime UsedAt { get; set; } = DateTime.Now;

    public DateTime? RefundedAt { get; set; }
}
