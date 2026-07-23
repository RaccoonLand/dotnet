using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Tests.Support;

/// <summary>Configurable <see cref="ICurrentExecutionContext"/> for audit/outbox user-stamping tests.</summary>
internal sealed class FakeExecutionContext : ICurrentExecutionContext
{
    public bool IsAvailable { get; init; } = true;

    public string? UserId { get; init; }

    public string? TenantId { get; init; }

    public string? CorrelationId { get; init; }
}
