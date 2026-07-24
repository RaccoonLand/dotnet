using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots.
/// DDD rules implemented here:
///  1) Holds DomainEvents to be persisted via the Outbox pattern on SaveChanges.
///  2) Holds ServiceEvents to be persisted via the Outbox pattern on SaveChanges.
///  3) Exposes a concurrency-management field (<see cref="ConcurrencyToken"/>) of type GUID.
/// <para>
/// Mutating operations (clearing/removing events, regenerating the concurrency token) are exposed
/// through the internal <see cref="IAggregateRootMutations"/> contract only. Business code cannot
/// invoke them by holding an <see cref="AggregateRoot{TKey}"/> or <see cref="IAggregateRoot"/>
/// reference — preventing silent event loss.
/// </para>
/// </summary>
public abstract class AggregateRoot<TKey> : Entity<TKey>, IAggregateRoot, IAggregateRootMutations
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
    /// (by the infrastructure through <see cref="IAggregateRootMutations.RegenerateConcurrencyToken"/>)
    /// to detect concurrent modifications. This value must also be present in every Query output.
    /// </summary>
    public Guid ConcurrencyToken { get; private set; } = Guid.CreateVersion7();

    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public IReadOnlyCollection<ServiceEvent> ServiceEvents => _serviceEvents.AsReadOnly();

    protected void RaiseDomainEvent(DomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    protected void RaiseServiceEvent(ServiceEvent serviceEvent) => _serviceEvents.Add(serviceEvent);

    // Explicit interface implementations keep the mutating surface off the public class API — the
    // only way to call these is through an IAggregateRootMutations reference, which is internal
    // and therefore visible only to InternalsVisibleTo-approved assemblies (persistence + tests).
    void IAggregateRootMutations.ClearDomainEvents() => _domainEvents.Clear();

    void IAggregateRootMutations.ClearServiceEvents() => _serviceEvents.Clear();

    void IAggregateRootMutations.RemoveDomainEvents(IReadOnlyCollection<Guid> eventIds)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        if (eventIds.Count == 0)
        {
            return;
        }

        var ids = eventIds as HashSet<Guid> ?? eventIds.ToHashSet();
        _domainEvents.RemoveAll(domainEvent => ids.Contains(domainEvent.EventId));
    }

    void IAggregateRootMutations.RemoveServiceEvents(IReadOnlyCollection<Guid> eventIds)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        if (eventIds.Count == 0)
        {
            return;
        }

        var ids = eventIds as HashSet<Guid> ?? eventIds.ToHashSet();
        _serviceEvents.RemoveAll(serviceEvent => ids.Contains(serviceEvent.EventId));
    }

    void IAggregateRootMutations.RegenerateConcurrencyToken()
        => ConcurrencyToken = Guid.CreateVersion7();
}
