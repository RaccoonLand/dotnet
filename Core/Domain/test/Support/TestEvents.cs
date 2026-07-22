using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Core.Domain.Tests.Support;

internal sealed record TestDomainEvent(Guid AggregateBusinessKey) : DomainEvent
{
    public override string EventType => "test.domain.event";
}

internal sealed record RenamedClrDomainEvent(Guid AggregateBusinessKey) : DomainEvent
{
    public override string EventType => "test.domain.event";
}

internal sealed record TestServiceEvent(Guid AggregateBusinessKey) : ServiceEvent
{
    public override string EventType => "test.service.event";
}

internal sealed record VersionedServiceEvent(Guid AggregateBusinessKey) : ServiceEvent
{
    public override string EventType => "test.service.event";

    public override int EventVersion => 2;
}

internal sealed record RenamedClrServiceEvent(Guid AggregateBusinessKey) : ServiceEvent
{
    public override string EventType => "test.service.event";
}
