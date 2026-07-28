namespace QuickBite.Services;

public sealed class OrderingOptions
{
    public const string SectionName = "Ordering";

    public decimal DeliveryFee { get; set; } = 15000;

    public decimal MinimumDeliverySubtotal { get; set; } = 30000;

    public int MaxOpenOrdersPerPhone { get; set; } = 3;
}
