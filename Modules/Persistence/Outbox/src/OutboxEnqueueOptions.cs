namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// Optional metadata when enqueueing an outbox message. Identity fields (<c>Id</c>, <c>CreatedBy</c>,
/// <c>OccurredOnUtc</c>) are always assigned by the framework and cannot be overridden here.
/// </summary>
public sealed class OutboxEnqueueOptions
{
    /// <summary>
    /// Logical event type stored on the message. Prefer an explicit, stable, versioned name
    /// (for example <c>order.placed.v1</c>). When null or whitespace, implementations may fall back to the
    /// payload CLR type's unqualified <c>Name</c> (not <c>FullName</c>) as an emergency default — that
    /// fallback is rename-fragile and should not be relied on in production contracts.
    /// </summary>
    public string? EventType { get; init; }

    /// <summary>Optional aggregate type name for correlation.</summary>
    public string? AggregateType { get; init; }

    /// <summary>Optional aggregate business key for correlation.</summary>
    public Guid? AggregateBusinessKey { get; init; }
}
