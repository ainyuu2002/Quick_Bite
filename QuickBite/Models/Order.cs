using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuickBite.Models;

public class Order
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập họ tên")]
    [StringLength(100)]
    public string CustomerName { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
    [RegularExpression(@"^0\d{8,10}$", ErrorMessage = "Số điện thoại không hợp lệ (9–11 chữ số, bắt đầu bằng 0)")]
    [StringLength(11)]
    public string Phone { get; set; } = null!;

    [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
    [StringLength(500)]
    public string Address { get; set; } = null!;

    [StringLength(500)]
    public string? Note { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Tổng tiền chốt tại thời điểm đặt (BR-03) = Σ UnitPrice × Quantity − DiscountAmount.</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal Total { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal DiscountAmount { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? PromotionId { get; set; }

    public Promotion? Promotion { get; set; }

    public int? VoucherId { get; set; }

    public Voucher? Voucher { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    /// <summary>
    /// Nhân viên đã bấm "Nhận đơn" (Pending → Accepted). Null = chưa ai nhận.
    /// Chốt một lần, không đổi khi đơn đi tiếp các trạng thái sau.
    /// </summary>
    public int? AcceptedByAccountId { get; set; }

    public Account? AcceptedByAccount { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
