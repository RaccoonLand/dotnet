namespace RaccoonLand.Core.Domain.Events;

/// <summary>
/// Base class for domain events (in-domain events).
/// These events are held inside an aggregate root and persisted via the Outbox pattern when
/// EF Core saves the aggregate. The default properties shared by all events are defined here.
/// </summary>
public abstract record DomainEvent
{
    /// <summary>Unique event identifier (used for idempotency when consuming the outbox).</summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <summary>The time the event occurred, in UTC.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Stable contract name used for serialization/routing in the outbox.
    /// Must not be derived from the CLR type name — rename of the class must not change this value.
    /// </summary>
    public abstract string EventType { get; }
}
