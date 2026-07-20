using RaccoonLand.Core.Domain.Events;

namespace CleanArchitectureSample.People.Domain.Events;

/// <summary>Cross-service integration event for person creation (published when ProcessServiceEvents is enabled).</summary>
public sealed record PersonCreatedIntegrationEvent(Guid PersonBusinessKey) : ServiceEvent
{
    public override string EventType => "person.created";
}
