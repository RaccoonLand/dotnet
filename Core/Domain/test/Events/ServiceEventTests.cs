using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Events;

public sealed class ServiceEventTests
{
    [Fact]
    public void EventVersion_DefaultsToOne()
    {
        ServiceEvent serviceEvent = new TestServiceEvent(Guid.CreateVersion7());

        Assert.Equal(1, serviceEvent.EventVersion);
    }

    [Fact]
    public void EventVersion_OverrideIsVisibleThroughBaseReference()
    {
        // Must use override (not new/hiding) so polymorphic readers see version 2.
        ServiceEvent asBase = new VersionedServiceEvent(Guid.CreateVersion7());
        var asDerived = (VersionedServiceEvent)asBase;

        Assert.Equal(2, asDerived.EventVersion);
        Assert.Equal(2, asBase.EventVersion);
    }

    [Fact]
    public void EventType_IsStableContract_SharedAcrossDifferentClrTypeNames()
    {
        // Same routing contract from two CLR types whose names differ from EventType.
        ServiceEvent first = new TestServiceEvent(Guid.CreateVersion7());
        ServiceEvent renamed = new RenamedClrServiceEvent(Guid.CreateVersion7());

        Assert.Equal("test.service.event", first.EventType);
        Assert.Equal(first.EventType, renamed.EventType);
        Assert.False(first.EventType.Contains(first.GetType().Name, StringComparison.Ordinal));
        Assert.False(renamed.EventType.Contains(renamed.GetType().Name, StringComparison.Ordinal));
    }

    [Fact]
    public void EventId_IsUnique_PerInstance()
    {
        var first = new TestServiceEvent(Guid.CreateVersion7());
        var second = new TestServiceEvent(Guid.CreateVersion7());

        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public void OccurredOnUtc_HasUtcOffset()
    {
        var serviceEvent = new TestServiceEvent(Guid.CreateVersion7());

        Assert.Equal(TimeSpan.Zero, serviceEvent.OccurredOnUtc.Offset);
    }
}
