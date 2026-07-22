using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Core.Domain.Tests.Support;

namespace RaccoonLand.Core.Domain.Tests.Events;

public sealed class DomainEventTests
{
    [Fact]
    public void EventId_IsUnique_PerInstance()
    {
        var first = new TestDomainEvent(Guid.CreateVersion7());
        var second = new TestDomainEvent(Guid.CreateVersion7());

        Assert.NotEqual(Guid.Empty, first.EventId);
        Assert.NotEqual(Guid.Empty, second.EventId);
        Assert.NotEqual(first.EventId, second.EventId);
    }

    [Fact]
    public void OccurredOnUtc_HasUtcOffset()
    {
        var domainEvent = new TestDomainEvent(Guid.CreateVersion7());

        Assert.Equal(TimeSpan.Zero, domainEvent.OccurredOnUtc.Offset);
    }

    [Fact]
    public void EventType_IsStableContract_SharedAcrossDifferentClrTypeNames()
    {
        // Same routing contract from two CLR types whose names differ from EventType.
        DomainEvent first = new TestDomainEvent(Guid.CreateVersion7());
        DomainEvent renamed = new RenamedClrDomainEvent(Guid.CreateVersion7());

        Assert.Equal("test.domain.event", first.EventType);
        Assert.Equal(first.EventType, renamed.EventType);
        Assert.False(first.EventType.Contains(first.GetType().Name, StringComparison.Ordinal));
        Assert.False(renamed.EventType.Contains(renamed.GetType().Name, StringComparison.Ordinal));
    }
}
