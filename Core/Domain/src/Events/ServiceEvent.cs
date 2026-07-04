namespace RaccoonLand.Core.Domain.Events;

/// <summary>
/// Base class for service events (cross-service integration events).
/// Like <see cref="DomainEvent"/> they are held inside an aggregate root and persisted via the
/// Outbox pattern on save, but their audience is outside the service boundary.
/// </summary>
public abstract record ServiceEvent
{
    /// <summary>Unique event identifier (used for idempotency on the consumer side).</summary>
    public Guid EventId { get; init; } = Guid.CreateVersion7();

    /// <summary>The time the event occurred, in UTC.</summary>
    public DateTimeOffset OccurredOnUtc { get; init; } = DateTimeOffset.UtcNow;

    /// <summary>The event type name; used for serialization/routing in the outbox.</summary>
    public string EventType => GetType().Name;

    /// <summary>Contract version of the event, to support schema evolution over time.</summary>
    public int EventVersion { get; init; } = 1;
}
