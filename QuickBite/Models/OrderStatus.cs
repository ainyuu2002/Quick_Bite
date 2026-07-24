namespace QuickBite.Models;

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

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next)
    {
        return next != OrderStatus.Cancelled
            && (int)next == (int)current + 1
            && current is not OrderStatus.Completed and not OrderStatus.Cancelled;
    }

    public static bool CanBeCancelledByCustomer(this OrderStatus current)
        => current == OrderStatus.Pending;

    public static bool CanBeCancelledByStaff(this OrderStatus current)
        => current == OrderStatus.Pending;
}
