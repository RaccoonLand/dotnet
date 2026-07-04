namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// Optional metadata when enqueueing an outbox message. Identity fields (<c>Id</c>, <c>CreatedBy</c>,
/// <c>OccurredOnUtc</c>) are always assigned by the framework and cannot be overridden here.
/// </summary>
public sealed class OutboxEnqueueOptions
{
    /// <summary>Overrides the default event type (payload CLR type name).</summary>
    public string? EventType { get; init; }

    /// <summary>Optional aggregate type name for correlation.</summary>
    public string? AggregateType { get; init; }

    /// <summary>Optional aggregate business key for correlation.</summary>
    public Guid? AggregateBusinessKey { get; init; }
}
