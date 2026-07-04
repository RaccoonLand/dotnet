namespace RaccoonLand.Modules.Persistence.Outbox.Abstraction;

/// <summary>
/// Request-scoped collector for outbox messages. Enqueue during endpoint/command handling; rows are written
/// atomically with the current EF Core transaction when <c>SaveChanges</c> completes.
/// </summary>
public interface IOutboxWriter
{
    /// <summary>
    /// Queues a message for the outbox channel identified by <typeparamref name="TOutbox"/>. The channel
    /// must be registered with <c>AddRaccoonLandOutbox&lt;TOutbox&gt;()</c>.
    /// </summary>
    /// <typeparam name="TOutbox">The channel marker.</typeparam>
    /// <param name="payload">The message body, serialized to JSON on flush.</param>
    /// <param name="options">Optional correlation metadata (event type, aggregate type/key). Identity fields are assigned by the framework.</param>
    void Enqueue<TOutbox>(object payload, OutboxEnqueueOptions? options = null)
        where TOutbox : IOutbox;
}
