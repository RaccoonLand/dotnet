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

    /// <summary>
    /// Stable contract name used for serialization/routing in the outbox and by external consumers.
    /// Must not be derived from the CLR type name — rename of the class must not change this value.
    /// </summary>
    public abstract string EventType { get; }

    /// <summary>
    /// Contract version of the event, to support schema evolution over time.
    /// Override in derived events when the published contract changes; do not hide this property.
    /// </summary>
    public virtual int EventVersion => 1;
}
