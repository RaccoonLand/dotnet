using RaccoonLand.Core.Domain.Events;

namespace CleanArchitectureSample.People.Domain.Events;

/// <summary>Raised when a person aggregate is created (in-service).</summary>
public sealed record PersonCreated(Guid PersonBusinessKey) : DomainEvent
{
    public override string EventType => "person.created";
}
