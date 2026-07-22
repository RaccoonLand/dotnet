using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Core.Domain.Tests.Support;

internal sealed class TestAggregateRoot : AggregateRoot<int>
{
    public TestAggregateRoot()
    {
    }

    public TestAggregateRoot(int id)
        : base(id)
    {
    }

    public void RaiseDomain(DomainEvent domainEvent) => RaiseDomainEvent(domainEvent);

    public void RaiseService(ServiceEvent serviceEvent) => RaiseServiceEvent(serviceEvent);
}
