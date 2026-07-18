using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots.
/// DDD rules implemented here:
///  1) Holds DomainEvents to be persisted via the Outbox pattern on SaveChanges.
///  2) Holds ServiceEvents to be persisted via the Outbox pattern on SaveChanges.
///  3) Exposes a concurrency-management field (<see cref="ConcurrencyToken"/>) of type GUID.
/// </summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot
{
    private readonly List<DomainEvent> _domainEvents = [];
    private readonly List<ServiceEvent> _serviceEvents = [];

    protected AggregateRoot(TKey id) : base(id)
    {
    }

    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Concurrency token. Configured as an EF Core concurrency token and regenerated on every save
    /// to detect concurrent modifications. This value must also be present in every Query output.
    /// </summary>
    public Guid ConcurrencyToken { get; private set; } = Guid.CreateVersion7();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<ServiceEvent> ServiceEvents => _serviceEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void ClearServiceEvents() => _serviceEvents.Clear();

    public void RemoveDomainEvents(IReadOnlyCollection<Guid> eventIds)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        if (eventIds.Count == 0)
        {
            return;
        }

        var ids = eventIds as HashSet<Guid> ?? eventIds.ToHashSet();
        _domainEvents.RemoveAll(domainEvent => ids.Contains(domainEvent.EventId));
    }

    public void RemoveServiceEvents(IReadOnlyCollection<Guid> eventIds)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        if (eventIds.Count == 0)
        {
            return;
        }

        var ids = eventIds as HashSet<Guid> ?? eventIds.ToHashSet();
        _serviceEvents.RemoveAll(serviceEvent => ids.Contains(serviceEvent.EventId));
    }

    public void RegenerateConcurrencyToken() => ConcurrencyToken = Guid.CreateVersion7();

    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RaiseServiceEvent(ServiceEvent serviceEvent) => _serviceEvents.Add(serviceEvent);
}