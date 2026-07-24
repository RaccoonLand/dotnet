using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Non-generic aggregate-root contract. It lets the infrastructure layer detect aggregates
/// and read their outbox-bound state without knowing the key type.
/// <para>
/// This surface is intentionally <b>read-only</b>: mutating an aggregate's persisted state
/// (clearing events, removing published events, regenerating the concurrency token) is an
/// infrastructure concern exposed via the internal <c>IAggregateRootMutations</c> contract.
/// Business code should never need those methods and cannot invoke them through
/// <see cref="IAggregateRoot"/>.
/// </para>
/// </summary>
public interface IAggregateRoot
{
    /// <summary>Stable business key of the aggregate; used as the source identifier on outbox rows.</summary>
    Guid BusinessKey { get; }

    /// <summary>Optimistic-concurrency token.</summary>
    Guid ConcurrencyToken { get; }

    /// <summary>Pending domain events raised by the aggregate, awaiting outbox persistence.</summary>
    IReadOnlyCollection<DomainEvent> DomainEvents { get; }

    /// <summary>Pending service events raised by the aggregate, awaiting outbox persistence.</summary>
    IReadOnlyCollection<ServiceEvent> ServiceEvents { get; }
}
