using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.RabbitMQ.Tests.Support;

public sealed record TestServiceEvent(Guid AggregateBusinessKey) : ServiceEvent
{
    public override string EventType => "test.service.event";
}

public sealed class ServiceHandlerRecorder
{
    public List<Guid> Handled { get; } = [];

    public List<string> Order { get; } = [];
}

public sealed class FirstRecordingServiceHandler(ServiceHandlerRecorder recorder)
    : IServiceEventHandler<TestServiceEvent>
{
    public Task HandleAsync(
        TestServiceEvent serviceEvent,
        ServiceEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        recorder.Handled.Add(serviceEvent.EventId);
        recorder.Order.Add(nameof(FirstRecordingServiceHandler));
        return Task.CompletedTask;
    }
}

public sealed class SecondRecordingServiceHandler(ServiceHandlerRecorder recorder)
    : IServiceEventHandler<TestServiceEvent>
{
    public Task HandleAsync(
        TestServiceEvent serviceEvent,
        ServiceEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        recorder.Order.Add(nameof(SecondRecordingServiceHandler));
        return Task.CompletedTask;
    }
}

public sealed class ThrowingServiceHandler(ServiceHandlerRecorder recorder)
    : IServiceEventHandler<TestServiceEvent>
{
    public async Task HandleAsync(
        TestServiceEvent serviceEvent,
        ServiceEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        recorder.Order.Add(nameof(ThrowingServiceHandler));
        await Task.Yield();
        throw new InvalidOperationException("service handler failed");
    }
}
