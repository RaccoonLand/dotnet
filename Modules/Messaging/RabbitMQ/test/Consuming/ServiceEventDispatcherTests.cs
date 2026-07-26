using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using RaccoonLand.Modules.Messaging.Abstractions;
using RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Support;
using Xunit;

namespace RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Consuming;

public sealed class ServiceEventDispatcherTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    private const string EventType = "test.service.event";

    private static ServiceEventHandlerRegistration Registration => new()
    {
        EventType = EventType,
        EventClrType = typeof(TestServiceEvent),
        HandlerServiceType = typeof(IServiceEventHandler<TestServiceEvent>),
    };

    private static ServiceEventMessage ToMessage(TestServiceEvent serviceEvent, Guid? envelopeEventId = null) => new()
    {
        EventId = envelopeEventId ?? serviceEvent.EventId,
        EventType = EventType,
        Payload = JsonSerializer.Serialize(serviceEvent, WebJson),
    };

    private static ServiceEventDispatcher CreateDispatcher(
        IServiceProvider provider,
        params ServiceEventHandlerRegistration[] registrations)
    {
        var registry = new ServiceEventHandlerRegistry();
        foreach (var registration in registrations)
        {
            registry.Add(registration);
        }

        return new ServiceEventDispatcher(
            provider.GetRequiredService<IServiceScopeFactory>(),
            registry,
            NullLogger<ServiceEventDispatcher>.Instance);
    }

    [Fact]
    public async Task DispatchAsync_InvokesAllHandlersInOrder()
    {
        var recorder = new ServiceHandlerRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IServiceEventHandler<TestServiceEvent>, FirstRecordingServiceHandler>();
        services.AddScoped<IServiceEventHandler<TestServiceEvent>, SecondRecordingServiceHandler>();
        using var provider = services.BuildServiceProvider();

        var serviceEvent = new TestServiceEvent(Guid.NewGuid());
        var dispatcher = CreateDispatcher(provider, Registration);

        await dispatcher.DispatchAsync(ToMessage(serviceEvent));

        Assert.Equal(
            [nameof(FirstRecordingServiceHandler), nameof(SecondRecordingServiceHandler)],
            recorder.Order);
        Assert.Equal([serviceEvent.EventId], recorder.Handled);
    }

    [Fact]
    public async Task DispatchAsync_WhenNoRegistration_Throws()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(ToMessage(new TestServiceEvent(Guid.NewGuid()))));
    }

    [Fact]
    public async Task DispatchAsync_WhenNoHandlerInDi_Throws()
    {
        using var provider = new ServiceCollection().BuildServiceProvider();
        var dispatcher = CreateDispatcher(provider, Registration);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(ToMessage(new TestServiceEvent(Guid.NewGuid()))));
    }

    [Fact]
    public async Task DispatchAsync_WhenPayloadEventIdMismatchesEnvelope_Throws()
    {
        var recorder = new ServiceHandlerRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IServiceEventHandler<TestServiceEvent>, FirstRecordingServiceHandler>();
        using var provider = services.BuildServiceProvider();

        var dispatcher = CreateDispatcher(provider, Registration);
        var message = ToMessage(new TestServiceEvent(Guid.NewGuid()), envelopeEventId: Guid.NewGuid());

        await Assert.ThrowsAsync<InvalidOperationException>(() => dispatcher.DispatchAsync(message));
        Assert.Empty(recorder.Handled);
    }

    [Fact]
    public async Task DispatchAsync_WhenHandlerThrows_PropagatesUnwrappedException()
    {
        var recorder = new ServiceHandlerRecorder();
        var services = new ServiceCollection();
        services.AddSingleton(recorder);
        services.AddScoped<IServiceEventHandler<TestServiceEvent>, ThrowingServiceHandler>();
        using var provider = services.BuildServiceProvider();

        var dispatcher = CreateDispatcher(provider, Registration);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => dispatcher.DispatchAsync(ToMessage(new TestServiceEvent(Guid.NewGuid()))));
        Assert.Equal("service handler failed", ex.Message);
    }
}
