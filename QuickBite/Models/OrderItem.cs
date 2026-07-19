using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int MenuItemId { get; set; }
    public MenuItem? MenuItem { get; set; }

    public int Quantity { get; set; }

    /// <summary>
    /// Giá CHỐT tại thời điểm đặt (BR-02) — admin đổi giá món sau đó
    /// không ảnh hưởng đơn cũ. Không join lấy MenuItem.Price khi hiển thị đơn.
    /// </summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal UnitPrice { get; set; }

    [NotMapped]
    public decimal LineTotal => UnitPrice * Quantity;
}
