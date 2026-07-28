namespace QuickBite.Models;

public enum OrderType
{
    Delivery = 0,
    Pickup = 1
}

public static class OrderTypeExtensions
{
    public static string ToDisplayText(this OrderType type) => type switch
    {
        OrderType.Delivery => "Giao hàng",
        OrderType.Pickup => "Nhận tại quán",
        _ => type.ToString()
    };
}
