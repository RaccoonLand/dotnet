using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Messaging.OutboxRelay;

/// <summary>
/// Maps a stable <c>EventType</c> contract string to the CLR <see cref="DomainEvent"/> type and the
/// open handler service type <c>IDomainEventHandler&lt;TEvent&gt;</c> used for DI resolution.
/// </summary>
public sealed class DomainEventHandlerRegistration
{
    public required string EventType { get; init; }

    public required Type EventClrType { get; init; }

    /// <summary>Closed generic handler service type, for example <c>IDomainEventHandler&lt;OrderSubmitted&gt;</c>.</summary>
    public required Type HandlerServiceType { get; init; }
}
