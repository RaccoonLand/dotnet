namespace RaccoonLand.Modules.Messaging.OutboxRelay;

/// <summary>
/// Registry of domain-event handler registrations keyed by stable <c>EventType</c>.
/// </summary>
public sealed class DomainEventHandlerRegistry
{
    private readonly Dictionary<string, DomainEventHandlerRegistration> _byEventType =
        new(StringComparer.Ordinal);

    public void Add(DomainEventHandlerRegistration registration)
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

    public bool TryGet(string eventType, out DomainEventHandlerRegistration registration)
        => _byEventType.TryGetValue(eventType, out registration!);

    public IReadOnlyCollection<DomainEventHandlerRegistration> All => _byEventType.Values;
}
