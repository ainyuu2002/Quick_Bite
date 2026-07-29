namespace QuickBite.Services;

public sealed class OrderingOptions
{
    public const string SectionName = "Ordering";

    public decimal DeliveryFee { get; set; } = 15000;

    public decimal MinimumDeliverySubtotal { get; set; } = 30000;

    public int MaxOpenOrdersPerPhone { get; set; } = 3;

    public int PendingExpiryMinutes { get; set; } = 15;

    public int ReadyNoShowMinutes { get; set; } = 60;

    public int SweepIntervalSeconds { get; set; } = 60;

    public decimal PartyThreshold { get; set; } = 1000000;

    public int PartyDepositPercent { get; set; } = 40;

    public int PartyMinLeadHours { get; set; } = 24;

    public int PartyDepositTimeoutMinutes { get; set; } = 30;

    public int PartyReviewTimeoutHours { get; set; } = 24;
}
