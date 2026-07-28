using QuickBite.Models;

namespace QuickBite.Modules.Operations.MenuAvailability;

/// <summary>
/// Quota và khung giờ bán của một món. ReservedQuantity được reset khi QuotaDate
/// khác ngày kinh doanh hiện tại.
/// </summary>
public sealed class DailyQuota
{
    public const int DefaultDailyLimit = 30;

    public int Id { get; set; }

    public int MenuItemId { get; set; }

    public MenuItem MenuItem { get; set; } = null!;

    public int DailyLimit { get; set; } = DefaultDailyLimit;

    public int ReservedQuantity { get; set; }

    public DateOnly QuotaDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    public TimeOnly? SaleStartsAt { get; set; }

    public TimeOnly? SaleEndsAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public int? UpdatedByAccountId { get; set; }

    public void ResetFor(DateOnly businessDate)
    {
        if (QuotaDate == businessDate)
        {
            return;
        }

        QuotaDate = businessDate;
        ReservedQuantity = 0;
    }

    public bool IsWithinSaleWindow(TimeOnly currentTime)
    {
        if (SaleStartsAt is null || SaleEndsAt is null || SaleStartsAt == SaleEndsAt)
        {
            return true;
        }

        return SaleStartsAt < SaleEndsAt
            ? currentTime >= SaleStartsAt && currentTime < SaleEndsAt
            : currentTime >= SaleStartsAt || currentTime < SaleEndsAt;
    }

    public int RemainingQuantity => Math.Max(0, DailyLimit - ReservedQuantity);
}
