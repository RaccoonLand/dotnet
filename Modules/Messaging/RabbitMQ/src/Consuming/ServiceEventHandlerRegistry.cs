namespace RaccoonLand.Modules.Messaging.RabbitMQ;

/// <summary>
/// Registry of service-event handler registrations keyed by stable <c>EventType</c>.
/// </summary>
public sealed class ServiceEventHandlerRegistry
{
    private readonly Dictionary<string, ServiceEventHandlerRegistration> _byEventType =
        new(StringComparer.Ordinal);

    public void Add(ServiceEventHandlerRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration);
        ArgumentException.ThrowIfNullOrWhiteSpace(registration.EventType);

        if (_byEventType.TryGetValue(registration.EventType, out var existing))
        {
            if (existing.EventClrType != registration.EventClrType
                || existing.HandlerServiceType != registration.HandlerServiceType)
            {
                throw new InvalidOperationException(
                    $"EventType '{registration.EventType}' is already mapped to " +
                    $"{existing.EventClrType.FullName} / {existing.HandlerServiceType.FullName}.");
            }

            return;
        }

        _byEventType.Add(registration.EventType, registration);
    }

    public bool TryGet(string eventType, out ServiceEventHandlerRegistration registration)
        => _byEventType.TryGetValue(eventType, out registration!);

    public IReadOnlyCollection<ServiceEventHandlerRegistration> All => _byEventType.Values;
}
