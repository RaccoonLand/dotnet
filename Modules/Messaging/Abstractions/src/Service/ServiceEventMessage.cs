namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Transport-neutral envelope for a received service event (AMQP body + headers mapped by the consumer).
/// </summary>
public sealed class ServiceEventMessage
{
    public Guid EventId { get; init; }

    public required string EventType { get; init; }

    public int? EventVersion { get; init; }

    /// <summary>JSON payload (serialized <c>ServiceEvent</c>).</summary>
    public required string Payload { get; init; }

    public string? AggregateType { get; init; }

    public Guid? AggregateBusinessKey { get; init; }

    public DateTimeOffset? OccurredOnUtc { get; init; }

    public string? CreatedBy { get; init; }

    /// <summary>AMQP routing key when available.</summary>
    public string? RoutingKey { get; init; }
}
