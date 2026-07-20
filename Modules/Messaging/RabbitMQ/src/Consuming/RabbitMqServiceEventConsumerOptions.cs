namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Options for the RabbitMQ service-event consumer hosted service.
/// </summary>
public sealed class RabbitMqServiceEventConsumerOptions
{
    /// <summary>Default root configuration section name (<c>RabbitMqServiceEventConsumer</c>).</summary>
    public const string SectionName = "RabbitMqServiceEventConsumer";

    /// <summary>
    /// Optional AMQP URI. When set, it takes precedence over discrete host credentials.
    /// </summary>
    public string? Uri { get; set; }

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>Exchange to bind the consumer queue to (must match the publisher).</summary>
    public string ExchangeName { get; set; } = "raccoonland.service-events";

    /// <summary>AMQP exchange type. Defaults to <c>topic</c>.</summary>
    public string ExchangeType { get; set; } = "topic";

    public bool DurableExchange { get; set; } = true;

    /// <summary>Required queue name for this consuming service.</summary>
    public string QueueName { get; set; } = string.Empty;

    public bool DurableQueue { get; set; } = true;

    /// <summary>
    /// Routing key patterns bound to the queue (topic). Default <c>#</c> receives all service events.
    /// </summary>
    public string[] BindingKeys { get; set; } = ["#"];

    public ushort PrefetchCount { get; set; } = 10;

    /// <summary>When true, declares exchange, queue, and bindings on startup.</summary>
    public bool DeclareTopology { get; set; } = true;

    /// <summary>Inbox claim lease passed to <c>IInboxStore.TryClaimAsync</c>.</summary>
    public TimeSpan InboxClaimLease { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// When true, failed handler processing nacks with requeue (legacy). Prefer
    /// <see cref="MaxDeliveryAttempts"/> + dead-letter topology for controlled retries.
    /// Ignored when <see cref="MaxDeliveryAttempts"/> is greater than zero (retry uses republish).
    /// </summary>
    public bool RequeueOnFailure { get; set; } = true;

    /// <summary>
    /// Maximum application-level handler attempts before the message is dead-lettered.
    /// Counted in the <c>raccoonland-delivery-attempt</c> header (starts at 1) when this consumer
    /// republishes after a failure — not the broker's <c>redelivered</c> / <c>x-death</c> counters.
    /// When greater than zero, a dead-letter exchange must be configured
    /// (<see cref="EnableDeadLetterTopology"/> or <see cref="DeadLetterExchangeName"/>).
    /// Set to <c>0</c> to disable attempt tracking and fall back to <see cref="RequeueOnFailure"/>.
    /// </summary>
    public int MaxDeliveryAttempts { get; set; } = 5;

    /// <summary>
    /// Delay before NACK+requeue when inbox returns <c>ClaimHeldByOther</c>, to avoid a hot loop
    /// against a live claim. Use <see cref="TimeSpan.Zero"/> only for tests.
    /// </summary>
    public TimeSpan ClaimHeldByOtherRequeueDelay { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>When true, declares a durable dead-letter exchange and queue and binds them.</summary>
    public bool EnableDeadLetterTopology { get; set; } = true;


    /// <summary>Dead-letter exchange name (fanout). Default derived from <see cref="QueueName"/> when empty.</summary>
    public string DeadLetterExchangeName { get; set; } = string.Empty;

    /// <summary>Dead-letter queue name. Default derived from <see cref="QueueName"/> when empty.</summary>
    public string DeadLetterQueueName { get; set; } = string.Empty;

    /// <summary>Routing key used when publishing a poisoned message to the DLX. Default <c>poison</c>.</summary>
    public string DeadLetterRoutingKey { get; set; } = "poison";

    public string? ClientProvidedName { get; set; } = "raccoonland-service-events-consumer";

    public string ResolveDeadLetterExchangeName()
        => string.IsNullOrWhiteSpace(DeadLetterExchangeName)
            ? $"{QueueName}.dlx"
            : DeadLetterExchangeName;

    public string ResolveDeadLetterQueueName()
        => string.IsNullOrWhiteSpace(DeadLetterQueueName)
            ? $"{QueueName}.poison"
            : DeadLetterQueueName;
}
