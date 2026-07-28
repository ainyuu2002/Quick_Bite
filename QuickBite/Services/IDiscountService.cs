namespace QuickBite.Services;

public sealed record DiscountResult(decimal DiscountAmount, string? RejectionReason)
{
    public bool IsApplied => RejectionReason is null && DiscountAmount > 0m;

    public static DiscountResult None { get; } = new(0m, null);
}

public interface IDiscountService
{
    Task<DiscountResult> ApplyAsync(
        decimal subtotal,
        string? promotionCode,
        string phone,
        CancellationToken cancellationToken = default);
}

public sealed class NoDiscountService : IDiscountService
{
    public Task<DiscountResult> ApplyAsync(
        decimal subtotal,
        string? promotionCode,
        string phone,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(promotionCode))
        {
            return Task.FromResult(DiscountResult.None);
        }

        return Task.FromResult(new DiscountResult(0m, "Tính năng khuyến mãi chưa được triển khai."));
    }
}
