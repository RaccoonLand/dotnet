using RaccoonLand.Modules.Messaging.Abstractions;
using RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Support;
using Xunit;

namespace RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Consuming;

public sealed class ServiceEventHandlerRegistryTests
{
    private static ServiceEventHandlerRegistration Registration => new()
    {
        EventType = "test.service.event",
        EventClrType = typeof(TestServiceEvent),
        HandlerServiceType = typeof(IServiceEventHandler<TestServiceEvent>),
    };

    [Fact]
    public void TryGet_AfterAdd_ReturnsRegistration()
    {
        var registry = new ServiceEventHandlerRegistry();
        registry.Add(Registration);

        Assert.True(registry.TryGet("test.service.event", out var found));
        Assert.Equal(typeof(TestServiceEvent), found.EventClrType);
    }

    [Fact]
    public void TryGet_WhenUnknown_ReturnsFalse()
    {
        var registry = new ServiceEventHandlerRegistry();

        Assert.False(registry.TryGet("missing", out _));
    }

    [Fact]
    public void Add_SameRegistrationTwice_IsIdempotent()
    {
        var registry = new ServiceEventHandlerRegistry();
        registry.Add(Registration);
        registry.Add(Registration);

        Assert.Single(registry.All);
    }

    [Fact]
    public void Add_ConflictingClrTypeForSameEventType_Throws()
    {
        var registry = new ServiceEventHandlerRegistry();
        registry.Add(Registration);

        var conflicting = new ServiceEventHandlerRegistration
        {
            EventType = "test.service.event",
            EventClrType = typeof(OtherServiceEvent),
            HandlerServiceType = typeof(IServiceEventHandler<OtherServiceEvent>),
        };

        Assert.Throws<InvalidOperationException>(() => registry.Add(conflicting));
    }

    private sealed record OtherServiceEvent : RaccoonLand.Core.Domain.Events.ServiceEvent
    {
        public override string EventType => "test.service.event";
    }
}
