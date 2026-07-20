using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Handles one concrete <typeparamref name="TEvent"/> raised inside the same service boundary.
/// Handlers run asynchronously in a worker (not in the originating HTTP/command request).
/// </summary>
/// <typeparam name="TEvent">Concrete domain event type.</typeparam>
public interface IDomainEventHandler<in TEvent>
    where TEvent : DomainEvent
{
    Task HandleAsync(
        TEvent domainEvent,
        DomainEventHandlingContext context,
        CancellationToken cancellationToken = default);
}
