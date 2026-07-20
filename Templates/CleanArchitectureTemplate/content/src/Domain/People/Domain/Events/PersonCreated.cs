using RaccoonLand.Core.Domain.Events;

namespace CleanArchitectureTemplate.People.Domain.Events;

public sealed record PersonCreated(Guid PersonBusinessKey) : DomainEvent
{
    public override string EventType => "person.created";
}
