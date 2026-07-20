namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Dispatches a claimed domain outbox row to all registered handlers for its <c>EventType</c>.
/// Deserialization and handler resolution are implementation details of the Messaging.OutboxRelay package.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Deserializes <paramref name="outboxEvent"/> and invokes matching handlers.
    /// Throws when no CLR type is registered for the event type, when deserialization fails,
    /// or when a handler throws — the relay must not mark the row processed in those cases.
    /// </summary>
    Task DispatchAsync(OutboxEventRecord outboxEvent, CancellationToken cancellationToken = default);
}
