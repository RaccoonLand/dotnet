namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// An outbox message waiting to be flushed with the ambient unit of work. Shares the same envelope shape as
/// domain/service outbox rows.
/// </summary>
public sealed record OutboxPendingMessage
{
    /// <summary>Message identifier (idempotency key); assigned by the framework.</summary>
    public required Guid Id { get; init; }

    /// <summary>
    /// Event type — from <see cref="OutboxEnqueueOptions.EventType"/> or the implementation's CLR-name fallback.
    /// </summary>
    public required string EventType { get; init; }

    /// <summary>Optional correlation: aggregate type name.</summary>
    public string? AggregateType { get; init; }

    /// <summary>Optional correlation: aggregate business key.</summary>
    public Guid? AggregateBusinessKey { get; init; }

    /// <summary>
    /// Message body as a <strong>JSON text</strong> string (already serialized). Implementations must treat
    /// this value as final JSON and must not serialize it again when writing to storage. Polymorphic
    /// serialization rules are defined by the implementation's serializer settings.
    /// </summary>
    public required string Payload { get; init; }

    /// <summary>
    /// Acting user or principal when known. <see langword="null"/> means no user context (background job,
    /// system operation, or unavailable execution context) and is allowed.
    /// </summary>
    public string? CreatedBy { get; init; }

    /// <summary>
    /// When the message was enqueued. Implementations must assign UTC with a zero offset
    /// (for example <see cref="DateTimeOffset.UtcNow"/>).
    /// </summary>
    public required DateTimeOffset OccurredOnUtc { get; init; }
}
