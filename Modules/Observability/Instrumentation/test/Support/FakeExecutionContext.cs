using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Modules.Observability.Instrumentation.Tests.Support;

/// <summary>Configurable <see cref="ICurrentExecutionContext"/> for context-enrichment tests.</summary>
internal sealed class FakeExecutionContext : ICurrentExecutionContext
{
    public bool IsAvailable { get; init; } = true;

    public string? UserId { get; init; }

    public string? TenantId { get; init; }

    public string? CorrelationId { get; init; }
}
