using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

/// <summary>An <see cref="IAuditable"/> entity that is not an aggregate root.</summary>
public sealed class AuditableRecord : Entity<Guid>
{
    public AuditableRecord() => Id = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;
}

/// <summary>An aggregate root (also <see cref="IAuditable"/>) used for audit + concurrency-token tests.</summary>
public sealed class TestAggregate : AggregateRoot<Guid>
{
    public TestAggregate() => Id = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;

    public void RaiseDomain(DomainEvent domainEvent) => RaiseDomainEvent(domainEvent);

    public void RaiseService(ServiceEvent serviceEvent) => RaiseServiceEvent(serviceEvent);
}

/// <summary>A plain entity that is neither <see cref="IAuditable"/> nor <see cref="IAggregateRoot"/>.</summary>
public sealed class PlainRecord
{
    public Guid Id { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = string.Empty;
}

public sealed record TestDomainEvent : DomainEvent
{
    public override string EventType => "test.domain.v1";

    public string Data { get; init; } = string.Empty;
}

public sealed record TestServiceEvent : ServiceEvent
{
    public override string EventType => "test.service.v1";

    public string Data { get; init; } = string.Empty;
}
