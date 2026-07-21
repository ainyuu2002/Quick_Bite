namespace QuickBite.Models;

/// <summary>
/// Trạng thái đơn hàng — chỉ được tiến đúng 1 bước (BR-01).
/// Pending → Accepted → Preparing → Ready → Completed.
/// Chính sách hủy được kiểm tra riêng bởi CanBeCancelledBy.
/// </summary>
public enum OrderStatus
{
    Pending = 0,    // Chờ xác nhận
    Accepted = 1,   // Đã nhận đơn
    Preparing = 2,  // Đang chuẩn bị
    Ready = 3,      // Sẵn sàng
    Completed = 4,  // Hoàn tất
    Cancelled = 5   // Đã hủy
}

public static class OrderStatusExtensions
{
    /// <summary>Tên hiển thị tiếng Việt cho UI.</summary>
    public static string ToDisplayText(this OrderStatus status) => status switch
    {
        OrderStatus.Pending   => "Chờ xác nhận",
        OrderStatus.Accepted  => "Đã nhận đơn",
        OrderStatus.Preparing => "Đang chuẩn bị",
        OrderStatus.Ready     => "Sẵn sàng",
        OrderStatus.Completed => "Hoàn tất",
        OrderStatus.Cancelled => "Đã hủy",
        _ => status.ToString()
    };

    /// <summary>
    /// Kiểm tra máy trạng thái (BR-01): chỉ cho tiến đúng 1 bước.
    /// Quyền hủy của khách và nhân viên là lớp chính sách riêng.
    /// </summary>
    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
    {
        return next != OrderStatus.Cancelled
            && (int)next == (int)current + 1
            && current is not OrderStatus.Completed and not OrderStatus.Cancelled;
    }

    /// <summary>
    /// Khách chỉ được hủy đơn khi quán chưa xác nhận.
    /// </summary>
    public static bool CanBeCancelledByCustomer(this OrderStatus current)
        => current == OrderStatus.Pending;

    /// <summary>
    /// Admin được hủy mọi đơn chưa kết thúc.
    /// </summary>
    public static bool CanBeCancelledByStaff(this OrderStatus current)
        => current is OrderStatus.Pending
            or OrderStatus.Accepted
            or OrderStatus.Preparing
            or OrderStatus.Ready;
}
