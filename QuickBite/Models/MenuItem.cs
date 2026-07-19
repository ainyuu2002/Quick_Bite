using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Models;

public class MenuItem
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên món không được để trống")]
    [StringLength(150)]
    public string Name { get; set; } = null!;

    [StringLength(500)]
    public string? Description { get; set; }

    [Range(1000, 10_000_000, ErrorMessage = "Giá phải từ 1.000đ trở lên")]
    [Column(TypeName = "decimal(18,0)")]
    public decimal Price { get; set; }

    [StringLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>false = hết hàng: vẫn hiện trên menu kèm nhãn, không thêm được vào giỏ (BR-04).</summary>
    public bool IsAvailable { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    // Quan hệ
    public int CategoryId { get; set; }
    public Category? Category { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();
}
