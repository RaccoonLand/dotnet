namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Publishes a claimed <see cref="OutboxEventCategory.Service"/> outbox row to an integration transport
/// (for example RabbitMQ). Broker-specific packages implement this port; the outbox relay depends only
/// on the abstraction.
/// </summary>
public interface IServiceEventPublisher
{
    /// <summary>
    /// Publishes the service event. On success the relay marks the outbox row processed.
    /// On failure the exception propagates and the row stays pending for retry.
    /// </summary>
    Task PublishAsync(OutboxEventRecord outboxEvent, CancellationToken cancellationToken = default);
}
