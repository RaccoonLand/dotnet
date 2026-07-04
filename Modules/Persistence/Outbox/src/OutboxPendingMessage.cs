namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// An outbox message waiting to be flushed on <c>SaveChanges</c>. Shares the same envelope shape as
/// domain/service outbox rows.
/// </summary>
public sealed record OutboxPendingMessage
{
    public required Guid Id { get; init; }

    public required string EventType { get; init; }

    public string? AggregateType { get; init; }

    public Guid? AggregateBusinessKey { get; init; }

    public required string Payload { get; init; }

    public string? CreatedBy { get; init; }

    public required DateTimeOffset OccurredOnUtc { get; init; }
}
