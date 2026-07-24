namespace RaccoonLand.Core.Domain.Abstractions;

/// <summary>
/// Infrastructure-only contract for the mutating operations on an aggregate root: clearing pending
/// events, removing events that have been persisted, and regenerating the optimistic-concurrency token.
/// <para>
/// This interface is <see langword="internal"/> and only visible to assemblies granted
/// <c>InternalsVisibleTo</c> access (RaccoonLand persistence packages and the domain test project).
/// This is deliberate: business code holding an aggregate reference must not be able to silently
/// discard pending events (which would drop unpublished domain/integration events) or bypass the
/// concurrency-token lifecycle.
/// </para>
/// <para>
/// Every <see cref="AggregateRoot{TKey}"/> implements this interface, so an infrastructure caller with
/// a public <see cref="IAggregateRoot"/> reference can obtain the mutation surface with a simple
/// pattern check: <c>if (entity is IAggregateRootMutations mutations) { ... }</c>.
/// </para>
/// </summary>
internal interface IAggregateRootMutations
{
    /// <summary>Removes every pending domain event. Only appropriate after successful persistence.</summary>
    void ClearDomainEvents();

    /// <summary>Removes every pending service event. Only appropriate after successful persistence.</summary>
    void ClearServiceEvents();

    /// <summary>Removes domain events whose <c>EventId</c> is in <paramref name="eventIds"/>.</summary>
    void RemoveDomainEvents(IReadOnlyCollection<Guid> eventIds);

    /// <summary>Removes service events whose <c>EventId</c> is in <paramref name="eventIds"/>.</summary>
    void RemoveServiceEvents(IReadOnlyCollection<Guid> eventIds);

    /// <summary>Regenerates the concurrency token; invoked by the save interceptor on every modification.</summary>
    void RegenerateConcurrencyToken();
}
