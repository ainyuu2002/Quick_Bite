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

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? Note { get; set; }

    public OrderType OrderType { get; set; } = OrderType.Delivery;

    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>Tổng tiền chốt tại thời điểm đặt (BR-03) = Σ UnitPrice × Quantity − DiscountAmount + DeliveryFee.</summary>
    [Column(TypeName = "decimal(18,0)")]
    public decimal Total { get; set; }

    // --- Order Core (giữ) ---
    [Column(TypeName = "decimal(18,0)")]
    public decimal DeliveryFee { get; set; }

    [NotMapped]
    public decimal Subtotal => Total - DeliveryFee;

    [Required]
    [StringLength(20)]
    public string OrderCode { get; set; } = null!;

    // --- Khuyến mãi / khách hàng (thêm từ dev) ---
    [Column(TypeName = "decimal(18,0)")]
    public decimal DiscountAmount { get; set; }

    public int? CustomerId { get; set; }

    public Customer? Customer { get; set; }

    public int? PromotionId { get; set; }

    public Promotion? Promotion { get; set; }

    public int? VoucherId { get; set; }

    public Voucher? Voucher { get; set; }

    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Cash;

    public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.Unpaid;

    public bool IsPartyOrder { get; set; }

    public DateTime? ScheduledFor { get; set; }

    [Column(TypeName = "decimal(18,0)")]
    public decimal DepositAmount { get; set; }

    public bool DepositPaid { get; set; }

    public DateTime? ApprovedAt { get; set; }

    public int? AcceptedByAccountId { get; set; }

    public Account? AcceptedByAccount { get; set; }

    public DateTime? AcceptedAt { get; set; }

    public List<OrderItem> Items { get; set; } = new();
}
