using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public enum ReasonKind
{
    Reject = 0,
    Cancel = 1,
    DeliveryFailed = 2,
    NoShow = 3
}

public class ReasonCatalog
{
    public int Id { get; set; }

    public ReasonKind Kind { get; set; }

    [Required, StringLength(200)]
    public string Text { get; set; } = null!;

    public int DisplayOrder { get; set; }

    public bool IsActive { get; set; } = true;
}

public static class ReasonKindExtensions
{
    public static string ToDisplayText(this ReasonKind kind) => kind switch
    {
        ReasonKind.Reject => "Từ chối đơn",
        ReasonKind.Cancel => "Khách hủy",
        ReasonKind.DeliveryFailed => "Giao thất bại",
        ReasonKind.NoShow => "Không đến lấy",
        _ => kind.ToString()
    };

    public static ReasonKind? ForStatus(OrderStatus status) => status switch
    {
        OrderStatus.Rejected => ReasonKind.Reject,
        OrderStatus.Cancelled => ReasonKind.Cancel,
        OrderStatus.DeliveryFailed => ReasonKind.DeliveryFailed,
        OrderStatus.NoShow => ReasonKind.NoShow,
        _ => null
    };
}
