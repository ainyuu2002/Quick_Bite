namespace QuickBite.Services.Events;

public interface IOrderEvent
{
    int OrderId { get; }
}

public sealed record OrderConfirmed(int OrderId) : IOrderEvent;

public sealed record OrderCompleted(int OrderId) : IOrderEvent;

public sealed record OrderCancelled(int OrderId, string? Reason) : IOrderEvent;

public sealed record OrderRejected(int OrderId, string? Reason) : IOrderEvent;

public sealed record OrderExpired(int OrderId) : IOrderEvent;

public interface IOrderEventHandler
{
    Task HandleAsync(IOrderEvent orderEvent, CancellationToken cancellationToken = default);
}

public interface IOrderEventPublisher
{
    Task PublishAsync(IOrderEvent orderEvent, CancellationToken cancellationToken = default);
}

public sealed class OrderEventPublisher : IOrderEventPublisher
{
    private readonly IEnumerable<IOrderEventHandler> _handlers;

    public OrderEventPublisher(IEnumerable<IOrderEventHandler> handlers)
    {
        _handlers = handlers;
    }

    public async Task PublishAsync(IOrderEvent orderEvent, CancellationToken cancellationToken = default)
    {
        foreach (var handler in _handlers)
        {
            await handler.HandleAsync(orderEvent, cancellationToken);
        }
    }
}
