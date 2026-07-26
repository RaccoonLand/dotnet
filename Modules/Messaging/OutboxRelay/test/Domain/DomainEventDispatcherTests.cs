using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RaccoonLand.Modules.Messaging.Abstractions;
using RaccoonLand.Modules.Messaging.OutboxRelay.Tests.Support;
using Xunit;

namespace RaccoonLand.Modules.Messaging.OutboxRelay.Tests.Domain;

public sealed class DomainEventDispatcherTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private const string EventType = "test.domain.event";

    private static DomainEventHandlerRegistration Registration => new()
    {
        EventType = EventType,
        EventClrType = typeof(TestDomainEvent),
        HandlerServiceType = typeof(IDomainEventHandler<TestDomainEvent>),
    };

    private static OutboxEventRecord ToOutbox(TestDomainEvent domainEvent, Guid? envelopeEventId = null) => new()
    {
        EventId = envelopeEventId ?? domainEvent.EventId,
        Category = OutboxEventCategory.Domain,
        EventType = EventType,
        AggregateType = nameof(TestDomainEvent),
        Payload = JsonSerializer.Serialize(domainEvent, WebJson),
    };

    private static DomainEventDispatcher CreateDispatcher(
        IServiceProvider provider,
        params DomainEventHandlerRegistration[] registrations)
    {
        var registry = new DomainEventHandlerRegistry();
        foreach (var registration in registrations)
        {
            registry.Add(registration);
        }

        return new DomainEventDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<DomainEventDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_InvokesAllRegisteredHandlersInOrder()
    {
        var recorder = new DomainHandlerRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IDomainEventHandler<TestDomainEvent>, FirstRecordingDomainHandler>();
        services.AddScoped<IDomainEventHandler<TestDomainEvent>, SecondRecordingDomainHandler>();
        using var provider = services.BuildServiceProvider();

        var domainEvent = new TestDomainEvent(Guid.NewGuid());
        var dispatcher = CreateDispatcher(provider, Registration);

        await dispatcher.DispatchAsync(ToOutbox(domainEvent));

        Assert.Equal(
            [nameof(FirstRecordingDomainHandler), nameof(SecondRecordingDomainHandler)],
            recorder.Order);
        Assert.Equal([domainEvent.EventId], recorder.Handled);
    }

    [Fact]
    public async Task DispatchAsync_WhenNoRegistration_Throws()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(ToOutbox(new TestDomainEvent(Guid.NewGuid()))));
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandlerInDi_Throws()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider, Registration);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(ToOutbox(new TestDomainEvent(Guid.NewGuid()))));
    }

    [Fact]
    public async Task DispatchAsync_WhenPayloadEventIdMismatchesEnvelope_Throws()
    {
        var recorder = new DomainHandlerRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IDomainEventHandler<TestDomainEvent>, FirstRecordingDomainHandler>();
        using var provider = services.BuildServiceProvider();

        var dispatcher = CreateDispatcher(provider, Registration);
        var outbox = ToOutbox(new TestDomainEvent(Guid.NewGuid()), envelopeEventId: Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(outbox));
        Assert.Empty(recorder.Handled);
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrows_PropagatesUnwrappedException()
    {
        var recorder = new DomainHandlerRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IDomainEventHandler<TestDomainEvent>, ThrowingDomainHandler>();
        using var provider = services.BuildServiceProvider();

        var dispatcher = CreateDispatcher(provider, Registration);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(ToOutbox(new TestDomainEvent(Guid.NewGuid()))));
        Assert.Equal("handler failed", ex.Message);
    }
}
