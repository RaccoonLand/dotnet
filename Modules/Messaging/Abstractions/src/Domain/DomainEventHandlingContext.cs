namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Context passed to domain-event handlers. Carries outbox metadata that is not always present on
/// the deserialized <c>DomainEvent</c> payload alone (for example aggregate type name).
/// </summary>
public sealed class DomainEventHandlingContext
{
    public required OutboxEventRecord OutboxEvent { get; init; }
}
