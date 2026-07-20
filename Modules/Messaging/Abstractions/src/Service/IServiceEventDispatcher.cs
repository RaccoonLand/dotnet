namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Dispatches a received <see cref="ServiceEventMessage"/> to registered
/// <see cref="IServiceEventHandler{TEvent}"/> instances.
/// </summary>
public interface IServiceEventDispatcher
{
    Task DispatchAsync(ServiceEventMessage message, CancellationToken cancellationToken = default);
}
