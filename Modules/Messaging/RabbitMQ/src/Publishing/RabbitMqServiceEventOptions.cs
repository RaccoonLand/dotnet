namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Options for publishing <c>Category=Service</c> outbox rows to RabbitMQ.
/// </summary>
public sealed class RabbitMqServiceEventOptions
{
    /// <summary>Default root configuration section name (<c>RabbitMqServiceEvents</c>).</summary>
    public const string SectionName = "RabbitMqServiceEvents";

    /// <summary>
    /// Optional AMQP URI (for example <c>amqp://user:pass@localhost:5672/</c>).
    /// When set, it takes precedence over discrete host credentials.
    /// </summary>
    public string? Uri { get; set; }

    public string HostName { get; set; } = "localhost";

    public int Port { get; set; } = 5672;

    public string UserName { get; set; } = "guest";

    public string Password { get; set; } = "guest";

    public string VirtualHost { get; set; } = "/";

    /// <summary>Exchange that receives service events (topic recommended).</summary>
    public string ExchangeName { get; set; } = "raccoonland.service-events";

    /// <summary>AMQP exchange type. Defaults to <c>topic</c>.</summary>
    public string ExchangeType { get; set; } = "topic";

    public bool DurableExchange { get; set; } = true;

    /// <summary>When true, the publisher declares the exchange on first use.</summary>
    public bool DeclareExchange { get; set; } = true;

    /// <summary>When true, published messages use persistent delivery mode.</summary>
    public bool PersistentMessages { get; set; } = true;

    /// <summary>
    /// Routing key template. Supported tokens: <c>{EventType}</c>, <c>{Category}</c>,
    /// <c>{AggregateType}</c>. Default is <c>{EventType}</c>.
    /// </summary>
    public string RoutingKeyFormat { get; set; } = "{EventType}";

    /// <summary>Client-provided connection name visible in the RabbitMQ management UI.</summary>
    public string? ClientProvidedName { get; set; } = "raccoonland-service-events-publisher";
}
