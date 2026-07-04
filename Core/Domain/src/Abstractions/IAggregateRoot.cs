using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Non-generic aggregate-root contract. It lets the infrastructure layer detect aggregates
/// and manage their events and concurrency token without knowing the key type.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Stable business key of the aggregate; used as the source identifier on outbox rows.</summary>
    Guid BusinessKey { get; }

    /// <summary>Optimistic-concurrency token.</summary>
    Guid ConcurrencyToken { get; }

    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    IReadOnlyCollection<ServiceEvent> ServiceEvents { get; }

    void ClearDomainEvents();

    void ClearServiceEvents();

    /// <summary>Regenerates the concurrency token; invoked by the interceptor on save.</summary>
    void RegenerateConcurrencyToken();
}
