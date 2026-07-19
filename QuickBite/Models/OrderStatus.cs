namespace QuickBite.Models;

/// <summary>
/// Trạng thái đơn hàng — chỉ được tiến đúng 1 bước (BR-01).
/// Pending → Accepted → Preparing → Ready → Completed.
/// Cancelled được phép từ mọi trạng thái trừ Completed.
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
    /// Kiểm tra máy trạng thái (BR-01): chỉ cho tiến đúng 1 bước,
    /// hoặc hủy khi chưa Completed. Dùng trong OrderService.ChangeStatus.
    /// </summary>
    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
    {
        if (next == OrderStatus.Cancelled)
            return current != OrderStatus.Completed && current != OrderStatus.Cancelled;

        return (int)next == (int)current + 1 && current != OrderStatus.Cancelled;
    }
}
