namespace QuickBite.Services;

public sealed record CustomerBadge(
    bool IsRegular,
    int CompletedOrders,
    bool HasEverBailed,
    string DisplayText);

public interface ICustomerInfoService
{
    Task<CustomerBadge?> GetBadgeAsync(
        string phone,
        CancellationToken cancellationToken = default);

    Task<bool> RequiresOtpAsync(
        string phone,
        decimal orderValue,
        CancellationToken cancellationToken = default);
}

public sealed class DefaultCustomerInfoService : ICustomerInfoService
{
    public Task<CustomerBadge?> GetBadgeAsync(
        string phone,
        CancellationToken cancellationToken = default)
        => Task.FromResult<CustomerBadge?>(null);

    public Task<bool> RequiresOtpAsync(
        string phone,
        decimal orderValue,
        CancellationToken cancellationToken = default)
        => Task.FromResult(false);
}
