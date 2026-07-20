namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// A pending (or recently claimed) row from the aggregate-event outbox table (<c>OutboxEvent</c>).
/// Property names align with the write-side columns; the primary key column <c>Id</c> is exposed as
/// <see cref="EventId"/> for consistency with inbox and service-event surfaces.
/// </summary>
public sealed class OutboxEventRecord
{
    private string _category = null!;
    private string _eventType = null!;
    private string _aggregateType = null!;
    private string _payload = null!;

    /// <summary>
    /// Event idempotency key. Maps from the outbox table column <c>Id</c> (same value as domain/service
    /// <c>EventId</c>).
    /// </summary>
    public Guid EventId { get; init; }

    /// <summary><see cref="OutboxEventCategory.Domain"/> or <see cref="OutboxEventCategory.Service"/>.</summary>
    public required string Category
    {
        get => _category;
        init => _category = OutboxEventCategory.EnsureKnown(value, nameof(Category));
    }

    /// <summary>Stable contract name from the event (<c>EventType</c>), not the CLR type name.</summary>
    public required string EventType
    {
        get => _eventType;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(EventType));
            _eventType = value;
        }
    }

    /// <summary>CLR type name of the aggregate that raised the event.</summary>
    public required string AggregateType
    {
        get => _aggregateType;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(AggregateType));
            _aggregateType = value;
        }
    }

    /// <summary>Stable business key of the aggregate.</summary>
    public Guid AggregateBusinessKey { get; init; }

    /// <summary>JSON payload of the event (web defaults).</summary>
    public required string Payload
    {
        get => _payload;
        init
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value, nameof(Payload));
            _payload = value;
        }
    }

    /// <summary>User that caused the event, when available from aggregate audit fields.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>When the event occurred (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; init; }

    /// <summary>When the outbox row was inserted (UTC).</summary>
    public DateTimeOffset CreatedOnUtc { get; init; }

    /// <summary>
    /// Claim stamp set by <see cref="IOutboxEventStore.ClaimPendingAsync"/>. Required for
    /// <see cref="IOutboxEventStore.MarkProcessedAsync"/>.
    /// </summary>
    public DateTimeOffset ClaimedOnUtc { get; init; }

    /// <summary>Builds the fencing token for <see cref="IOutboxEventStore.MarkProcessedAsync"/>.</summary>
    public OutboxClaim ToClaim() => new(EventId, ClaimedOnUtc);
}
