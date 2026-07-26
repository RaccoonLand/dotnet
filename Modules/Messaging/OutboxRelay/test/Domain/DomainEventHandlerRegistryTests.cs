using RaccoonLand.Modules.Messaging.OutboxRelay.Tests.Support;
using Xunit;

namespace RaccoonLand.Modules.Messaging.OutboxRelay.Tests.Domain;

public sealed class DomainEventHandlerRegistryTests
{
    private static DomainEventHandlerRegistration Registration(string eventType = "test.domain.event") => new()
    {
        EventType = eventType,
        EventClrType = typeof(TestDomainEvent),
        HandlerServiceType = typeof(RaccoonLand.Modules.Messaging.Abstractions.IDomainEventHandler<TestDomainEvent>),
    };

    [Fact]
    public void TryGet_AfterAdd_ReturnsRegistration()
    {
        var registry = new DomainEventHandlerRegistry();
        registry.Add(Registration());

        Assert.True(registry.TryGet("test.domain.event", out var found));
        Assert.Equal(typeof(TestDomainEvent), found.EventClrType);
    }

    [Fact]
    public void TryGet_WhenUnknown_ReturnsFalse()
    {
        var registry = new DomainEventHandlerRegistry();

        Assert.False(registry.TryGet("missing", out _));
    }

    [Fact]
    public void Add_SameRegistrationTwice_IsIdempotent()
    {
        var registry = new DomainEventHandlerRegistry();
        registry.Add(Registration());
        registry.Add(Registration());

        Assert.Single(registry.All);
    }

    [Fact]
    public void Add_ConflictingClrTypeForSameEventType_Throws()
    {
        var registry = new DomainEventHandlerRegistry();
        registry.Add(Registration());

        var conflicting = new DomainEventHandlerRegistration
        {
            EventType = "test.domain.event",
            EventClrType = typeof(OtherDomainEvent),
            HandlerServiceType = typeof(RaccoonLand.Modules.Messaging.Abstractions.IDomainEventHandler<OtherDomainEvent>),
        };

        Assert.Throws<InvalidOperationException>(() => registry.Add(conflicting));
    }

    private sealed record OtherDomainEvent : RaccoonLand.Core.Domain.Events.DomainEvent
    {
        public override string EventType => "test.domain.event";
    }
}
