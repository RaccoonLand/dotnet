using RaccoonLand.Core.Domain.Events;

namespace CapabilityCentricTemplate.People.Domain.Events;

public sealed record PersonCreatedIntegrationEvent(Guid PersonBusinessKey) : ServiceEvent
{
    public override string EventType => "person.created";
}
