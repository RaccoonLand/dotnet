namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Well-known AMQP application header names written by <see cref="RabbitMqServiceEventPublisher"/>.
/// Consumers should treat these as the integration contract alongside the JSON body.
/// </summary>
public static class ServiceEventMessageHeaders
{
    public const string EventId = "raccoonland-event-id";
    public const string EventType = "raccoonland-event-type";
    public const string EventVersion = "raccoonland-event-version";
    public const string Category = "raccoonland-category";
    public const string AggregateType = "raccoonland-aggregate-type";
    public const string AggregateBusinessKey = "raccoonland-aggregate-business-key";
    public const string OccurredOnUtc = "raccoonland-occurred-on-utc";
    public const string CreatedBy = "raccoonland-created-by";

    /// <summary>
    /// Application-level delivery attempt count (1-based) used for retry / poison decisions.
    /// Incremented by this consumer when it republishes after a handler failure. Not the same as
    /// RabbitMQ <c>redelivered</c> or <c>x-death</c> counters.
    /// </summary>
    public const string DeliveryAttempt = "raccoonland-delivery-attempt";

    /// <summary>Set when a message is published to the dead-letter exchange after exhausting retries.</summary>
    public const string Poisoned = "raccoonland-poisoned";
}
