using RaccoonLand.Core.Domain.Events;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.OutboxRelay.Tests.Support;

public sealed record TestDomainEvent(Guid AggregateBusinessKey) : DomainEvent
{
    public override string EventType => "test.domain.event";
}

/// <summary>Records handler invocations across the dispatcher's child scope.</summary>
public sealed class DomainHandlerRecorder
{
    public List<Guid> Handled { get; } = [];

    public List<string> Order { get; } = [];
}

public sealed class FirstRecordingDomainHandler(DomainHandlerRecorder recorder)
    : IDomainEventHandler<TestDomainEvent>
{
    public Task HandleAsync(
        TestDomainEvent domainEvent,
        DomainEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        recorder.Handled.Add(domainEvent.EventId);
        recorder.Order.Add(nameof(FirstRecordingDomainHandler));
        return Task.CompletedTask;
    }
}

public sealed class SecondRecordingDomainHandler(DomainHandlerRecorder recorder)
    : IDomainEventHandler<TestDomainEvent>
{
    public Task HandleAsync(
        TestDomainEvent domainEvent,
        DomainEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        recorder.Order.Add(nameof(SecondRecordingDomainHandler));
        return Task.CompletedTask;
    }
}

public sealed class ThrowingDomainHandler(DomainHandlerRecorder recorder)
    : IDomainEventHandler<TestDomainEvent>
{
    public async Task HandleAsync(
        TestDomainEvent domainEvent,
        DomainEventHandlingContext context,
        CancellationToken cancellationToken = default)
    {
        recorder.Order.Add(nameof(ThrowingDomainHandler));
        await Task.Yield();
        throw new InvalidOperationException("handler failed");
    }
}
