using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Handles one concrete <typeparamref name="TEvent"/> received from the integration transport
/// (for example RabbitMQ) on a consuming service.
/// </summary>
public interface IServiceEventHandler<in TEvent>
    where TEvent : ServiceEvent
{
    Task HandleAsync(
        TEvent serviceEvent,
        ServiceEventHandlingContext context,
        CancellationToken cancellationToken = default);
}
