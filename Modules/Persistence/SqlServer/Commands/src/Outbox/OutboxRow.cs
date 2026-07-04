namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>
/// A single row inserted into an outbox channel table. Property names match the column names used by the
/// Dapper INSERT statement.
/// </summary>
public sealed class OutboxRow
{
    public Guid Id { get; init; }

    public required string EventType { get; init; }

    public string? AggregateType { get; init; }

    public Guid? AggregateBusinessKey { get; init; }

    public required string Payload { get; init; }

    public string? CreatedBy { get; init; }

    public DateTimeOffset OccurredOnUtc { get; init; }

    public DateTimeOffset CreatedOnUtc { get; init; }
}
