namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// Request-scoped collector for outbox messages. Call <see cref="Enqueue{TOutbox}"/> during
/// endpoint/command handling; the implementation flushes the buffer when the ambient persistence
/// unit of work completes (typically EF Core <c>SaveChanges</c>).
/// </summary>
/// <remarks>
/// <para>
/// <b>Atomicity (conditional):</b> transactional all-or-nothing with entity changes is guaranteed only when
/// the implementation flushes on the <strong>same connection and transaction</strong> (or equivalent unit of
/// work) as those changes. The interface itself does not bind to <c>DbContext</c>; hosts must wire a
/// compliant implementation.
/// </para>
/// <para>
/// <b>Lifetime / concurrency:</b> scoped to one request (or one unit of work). Not safe for concurrent use of
/// the same instance. Do not register or resolve as a singleton for shared background work unless a specific
/// implementation documents otherwise. Does not deduplicate: each <c>Enqueue</c> becomes its own message.
/// Enqueue order within a scope is preserved as FIFO for that buffer.
/// </para>
/// </remarks>
public interface IOutboxWriter
{
    /// <summary>
    /// Queues a message for the outbox channel identified by <typeparamref name="TOutbox"/>. The channel
    /// must already be registered with the implementation.
    /// </summary>
    /// <typeparam name="TOutbox">The channel marker.</typeparam>
    /// <param name="payload">
    /// Non-null message body. The implementation serializes it to JSON for
    /// <see cref="OutboxPendingMessage.Payload"/>. Do not pass a string that is already JSON unless you
    /// intend that string to be JSON-encoded again as a JSON string value — prefer a typed DTO.
    /// </param>
    /// <param name="options">
    /// Optional correlation metadata. Prefer an explicit, stable, versioned
    /// <see cref="OutboxEnqueueOptions.EventType"/> (for example <c>order.placed.v1</c>). Identity fields
    /// (<c>Id</c>, <c>CreatedBy</c>, <c>OccurredOnUtc</c>) are assigned by the framework.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The channel was not registered.</exception>
    void Enqueue<TOutbox>(object payload, OutboxEnqueueOptions? options = null)
        where TOutbox : IOutbox;
}
