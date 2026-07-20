namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Context passed to service-event handlers on the consuming service.
/// </summary>
public sealed class ServiceEventHandlingContext
{
    public required ServiceEventMessage Message { get; init; }
}
