using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Maps a stable <c>EventType</c> to the CLR <see cref="ServiceEvent"/> type and handler service type.
/// </summary>
public sealed class ServiceEventHandlerRegistration
{
    public required string EventType { get; init; }

    public required Type EventClrType { get; init; }

    public required Type HandlerServiceType { get; init; }
}
