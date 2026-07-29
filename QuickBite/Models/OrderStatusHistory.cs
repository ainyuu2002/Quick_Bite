using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public class OrderStatusHistory
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public OrderStatus? FromStatus { get; set; }

    public OrderStatus ToStatus { get; set; }

    [StringLength(300)]
    public string? Reason { get; set; }

    public int? ChangedByAccountId { get; set; }
    public Account? ChangedByAccount { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.Now;
}
