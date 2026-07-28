namespace QuickBite.Models;

public enum OrderStatus
{
    Pending = 0,
    Confirmed = 1,
    Preparing = 2,
    Ready = 3,
    Completed = 4,
    Cancelled = 5,
    Rejected = 6,
    Delivering = 7,
    Expired = 8,
    DeliveryFailed = 9,
    NoShow = 10
}

public static class OrderStatusExtensions
{
    public static string ToDisplayText(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "Chờ xác nhận",
        OrderStatus.Confirmed => "Đã xác nhận",
        OrderStatus.Preparing => "Đang chuẩn bị",
        OrderStatus.Ready => "Sẵn sàng",
        OrderStatus.Delivering => "Đang giao",
        OrderStatus.Completed => "Hoàn tất",
        OrderStatus.Cancelled => "Khách đã hủy",
        OrderStatus.Rejected => "Đã từ chối",
        OrderStatus.Expired => "Quá hạn",
        OrderStatus.DeliveryFailed => "Giao thất bại",
        OrderStatus.NoShow => "Không đến lấy",
        _ => status.ToString()
    };

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next) => next switch
    {
        OrderStatus.Confirmed => current is OrderStatus.Pending,
        OrderStatus.Rejected => current is OrderStatus.Pending,
        OrderStatus.Cancelled => current is OrderStatus.Pending,
        OrderStatus.Expired => current is OrderStatus.Pending,
        OrderStatus.Preparing => current is OrderStatus.Confirmed,
        OrderStatus.Ready => current is OrderStatus.Preparing,
        OrderStatus.Delivering => current is OrderStatus.Ready,
        OrderStatus.Completed => current is OrderStatus.Ready or OrderStatus.Delivering,
        OrderStatus.DeliveryFailed => current is OrderStatus.Delivering,
        OrderStatus.NoShow => current is OrderStatus.Ready,
        _ => false
    };

    public static bool CanTransitionTo(this OrderStatus current, OrderStatus next, OrderType orderType)
    {
        if (!current.CanTransitionTo(next))
        {
            return false;
        }

        if (orderType == OrderType.Pickup && next == OrderStatus.Delivering)
        {
            return false;
        }

        if (orderType == OrderType.Delivery
            && current == OrderStatus.Ready
            && next == OrderStatus.Completed)
        {
            return false;
        }

        return true;
    }

    public static bool RequiresReason(this OrderStatus next)
        => next is OrderStatus.Rejected or OrderStatus.Cancelled
            or OrderStatus.DeliveryFailed or OrderStatus.NoShow;

    public static bool IsTerminal(this OrderStatus status)
        => status is OrderStatus.Completed or OrderStatus.Cancelled or OrderStatus.Rejected
            or OrderStatus.Expired or OrderStatus.DeliveryFailed or OrderStatus.NoShow;

    public static bool CanBeCancelledByCustomer(this OrderStatus current)
        => current == OrderStatus.Pending;
}
