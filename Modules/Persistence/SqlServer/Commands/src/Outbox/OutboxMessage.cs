namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>
/// A single row to be inserted into the outbox table. Property names match the column names used
/// by the Dapper INSERT statement so they can be bound by name.
/// </summary>
public sealed class OutboxMessage
{
    /// <summary>Primary key of the row; equals the event's own identifier (idempotency).</summary>
    public Guid Id { get; init; }

    /// <summary>"Domain" or "Service".</summary>
    public required string Category { get; init; }

    /// <summary>The CLR type name of the event.</summary>
    public required string EventType { get; init; }

    /// <summary>The CLR type name of the aggregate that raised the event.</summary>
    public required string AggregateType { get; init; }

    /// <summary>The stable business key of the aggregate that raised the event.</summary>
    public Guid AggregateBusinessKey { get; init; }

    /// <summary>Serialized event payload (JSON).</summary>
    public required string Payload { get; init; }

    /// <summary>
    /// The user that caused the event, taken from the aggregate's audit info. Useful for building a
    /// log/history later.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>When the event occurred (UTC).</summary>
    public DateTimeOffset OccurredOnUtc { get; init; }

    /// <summary>When the row was created (UTC).</summary>
    public DateTimeOffset CreatedOnUtc { get; init; }
}
