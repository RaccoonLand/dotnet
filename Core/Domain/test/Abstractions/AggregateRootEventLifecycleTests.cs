using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Abstractions;

public sealed class AggregateRootEventLifecycleTests
{
    [Fact]
    public void RaiseDomainEvent_RegistersOnlyDomainEvents()
    {
        var aggregate = new TestAggregateRoot(1);
        var domainEvent = new TestDomainEvent(aggregate.BusinessKey);

        aggregate.RaiseDomain(domainEvent);

        Assert.Single(aggregate.DomainEvents);
        Assert.Same(domainEvent, Assert.Single(aggregate.DomainEvents));
        Assert.Empty(aggregate.ServiceEvents);
    }

    [Fact]
    public void RaiseServiceEvent_RegistersOnlyServiceEvents()
    {
        var aggregate = new TestAggregateRoot(1);
        var serviceEvent = new TestServiceEvent(aggregate.BusinessKey);

        aggregate.RaiseService(serviceEvent);

        Assert.Single(aggregate.ServiceEvents);
        Assert.Same(serviceEvent, Assert.Single(aggregate.ServiceEvents));
        Assert.Empty(aggregate.DomainEvents);
    }

    [Fact]
    public void RaiseEvents_PreserveRegistrationOrder()
    {
        var aggregate = new TestAggregateRoot(1);
        var firstDomain = new TestDomainEvent(aggregate.BusinessKey);
        var secondDomain = new TestDomainEvent(aggregate.BusinessKey);
        var firstService = new TestServiceEvent(aggregate.BusinessKey);
        var secondService = new TestServiceEvent(aggregate.BusinessKey);

        aggregate.RaiseDomain(firstDomain);
        aggregate.RaiseDomain(secondDomain);
        aggregate.RaiseService(firstService);
        aggregate.RaiseService(secondService);

        Assert.Equal([firstDomain, secondDomain], aggregate.DomainEvents);
        Assert.Equal([firstService, secondService], aggregate.ServiceEvents);
    }

    [Fact]
    public void ClearDomainEvents_RemovesOnlyDomainEvents()
    {
        var aggregate = new TestAggregateRoot(1);
        var serviceEvent = new TestServiceEvent(aggregate.BusinessKey);
        aggregate.RaiseDomain(new TestDomainEvent(aggregate.BusinessKey));
        aggregate.RaiseService(serviceEvent);

        aggregate.ClearDomainEvents();

        Assert.Empty(aggregate.DomainEvents);
        Assert.Same(serviceEvent, Assert.Single(aggregate.ServiceEvents));
    }

    [Fact]
    public void ClearServiceEvents_RemovesOnlyServiceEvents()
    {
        var aggregate = new TestAggregateRoot(1);
        var domainEvent = new TestDomainEvent(aggregate.BusinessKey);
        aggregate.RaiseDomain(domainEvent);
        aggregate.RaiseService(new TestServiceEvent(aggregate.BusinessKey));

        aggregate.ClearServiceEvents();

        Assert.Same(domainEvent, Assert.Single(aggregate.DomainEvents));
        Assert.Empty(aggregate.ServiceEvents);
    }

    [Fact]
    public void RemoveDomainEvents_RemovesOnlyMatchingEventIds()
    {
        var aggregate = new TestAggregateRoot(1);
        var keep = new TestDomainEvent(aggregate.BusinessKey);
        var remove = new TestDomainEvent(aggregate.BusinessKey);
        var serviceEvent = new TestServiceEvent(aggregate.BusinessKey);
        aggregate.RaiseDomain(keep);
        aggregate.RaiseDomain(remove);
        aggregate.RaiseService(serviceEvent);

        aggregate.RemoveDomainEvents([remove.EventId]);

        Assert.Same(keep, Assert.Single(aggregate.DomainEvents));
        Assert.Same(serviceEvent, Assert.Single(aggregate.ServiceEvents));
    }

    [Fact]
    public void RemoveServiceEvents_RemovesOnlyMatchingEventIds()
    {
        var aggregate = new TestAggregateRoot(1);
        var keep = new TestServiceEvent(aggregate.BusinessKey);
        var remove = new TestServiceEvent(aggregate.BusinessKey);
        var domainEvent = new TestDomainEvent(aggregate.BusinessKey);
        aggregate.RaiseService(keep);
        aggregate.RaiseService(remove);
        aggregate.RaiseDomain(domainEvent);

        aggregate.RemoveServiceEvents([remove.EventId]);

        Assert.Same(keep, Assert.Single(aggregate.ServiceEvents));
        Assert.Same(domainEvent, Assert.Single(aggregate.DomainEvents));
    }

    [Fact]
    public void RemoveDomainEvents_WithEmptyCollection_IsNoOp()
    {
        var aggregate = new TestAggregateRoot(1);
        var domainEvent = new TestDomainEvent(aggregate.BusinessKey);
        aggregate.RaiseDomain(domainEvent);

        aggregate.RemoveDomainEvents([]);

        Assert.Same(domainEvent, Assert.Single(aggregate.DomainEvents));
    }

    [Fact]
    public void RemoveServiceEvents_WithEmptyCollection_IsNoOp()
    {
        var aggregate = new TestAggregateRoot(1);
        var serviceEvent = new TestServiceEvent(aggregate.BusinessKey);
        aggregate.RaiseService(serviceEvent);

        aggregate.RemoveServiceEvents([]);

        Assert.Same(serviceEvent, Assert.Single(aggregate.ServiceEvents));
    }

    [Fact]
    public void RemoveDomainEvents_Throws_WhenEventIdsIsNull()
    {
        var aggregate = new TestAggregateRoot(1);

        Assert.Throws<ArgumentNullException>(() => aggregate.RemoveDomainEvents(null!));
    }

    [Fact]
    public void RemoveServiceEvents_Throws_WhenEventIdsIsNull()
    {
        var aggregate = new TestAggregateRoot(1);

        Assert.Throws<ArgumentNullException>(() => aggregate.RemoveServiceEvents(null!));
    }

    [Fact]
    public void EventCollections_RejectExternalMutation_AndOnlyChangeViaAggregateApis()
    {
        var aggregate = new TestAggregateRoot(1);
        var domainEvent = new TestDomainEvent(aggregate.BusinessKey);
        var serviceEvent = new TestServiceEvent(aggregate.BusinessKey);
        aggregate.RaiseDomain(domainEvent);
        aggregate.RaiseService(serviceEvent);

        // Cast and mutation are asserted separately so a failed cast cannot masquerade as "read-only".
        var domainEvents = Assert.IsAssignableFrom<ICollection<RaccoonLand.Core.Domain.Events.DomainEvent>>(
            aggregate.DomainEvents);
        var serviceEvents = Assert.IsAssignableFrom<ICollection<RaccoonLand.Core.Domain.Events.ServiceEvent>>(
            aggregate.ServiceEvents);

        Assert.True(domainEvents.IsReadOnly);
        Assert.True(serviceEvents.IsReadOnly);
        Assert.Throws<NotSupportedException>(() =>
            domainEvents.Add(new TestDomainEvent(aggregate.BusinessKey)));
        Assert.Throws<NotSupportedException>(() =>
            serviceEvents.Add(new TestServiceEvent(aggregate.BusinessKey)));

        Assert.Same(domainEvent, Assert.Single(aggregate.DomainEvents));
        Assert.Same(serviceEvent, Assert.Single(aggregate.ServiceEvents));

        // Allowed mutation path remains Raise* / Clear* / Remove*.
        var extraDomain = new TestDomainEvent(aggregate.BusinessKey);
        aggregate.RaiseDomain(extraDomain);
        Assert.Equal([domainEvent, extraDomain], aggregate.DomainEvents);
    }
}
