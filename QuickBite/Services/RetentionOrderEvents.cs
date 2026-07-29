using QuickBite.Models;

namespace QuickBite.Services;

public interface IOrderEvents
{
    Task OrderCompletedAsync(Order order, CancellationToken cancellationToken = default);

    Task OrderCancelledAsync(Order order, CancellationToken cancellationToken = default);
}

public sealed class RetentionOrderEvents : IOrderEvents
{
    private readonly LoyaltyService _loyalty;
    private readonly IDiscountService _discounts;

    public RetentionOrderEvents(LoyaltyService loyalty, IDiscountService discounts)
    {
        _loyalty = loyalty;
        _discounts = discounts;
    }

    public Task OrderCompletedAsync(Order order, CancellationToken cancellationToken = default)
        => _loyalty.AccrueAsync(order, cancellationToken);

    public Task OrderCancelledAsync(Order order, CancellationToken cancellationToken = default)
        => _discounts.RefundAsync(order, cancellationToken);
}
