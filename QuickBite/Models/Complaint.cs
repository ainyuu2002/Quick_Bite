using System.ComponentModel.DataAnnotations;

namespace QuickBite.Models;

public enum ComplaintCategory
{
    Food = 0,
    MissingItem = 1,
    Delivery = 2,
    Attitude = 3,
    Other = 4
}

public enum ComplaintStatus
{
    New = 0,
    InProgress = 1,
    Resolved = 2
}

public class Complaint
{
    public int Id { get; set; }

    public int OrderId { get; set; }

    public Order? Order { get; set; }

    [Required, StringLength(11)]
    public string Phone { get; set; } = null!;

    public ComplaintCategory Category { get; set; }

    [Required(ErrorMessage = "Vui lòng mô tả vấn đề bạn gặp phải")]
    [StringLength(1000)]
    public string Description { get; set; } = null!;

    public ComplaintStatus Status { get; set; } = ComplaintStatus.New;

    [StringLength(1000)]
    public string? Resolution { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? ResolvedAt { get; set; }
}

public static class ComplaintExtensions
{
    public static string ToDisplayText(this ComplaintCategory category) => category switch
    {
        ComplaintCategory.Food => "Chất lượng món ăn",
        ComplaintCategory.MissingItem => "Thiếu món",
        ComplaintCategory.Delivery => "Giao hàng",
        ComplaintCategory.Attitude => "Thái độ phục vụ",
        ComplaintCategory.Other => "Khác",
        _ => category.ToString()
    };

    public static string ToDisplayText(this ComplaintStatus status) => status switch
    {
        ComplaintStatus.New => "Mới",
        ComplaintStatus.InProgress => "Đang xử lý",
        ComplaintStatus.Resolved => "Đã giải quyết",
        _ => status.ToString()
    };
}
