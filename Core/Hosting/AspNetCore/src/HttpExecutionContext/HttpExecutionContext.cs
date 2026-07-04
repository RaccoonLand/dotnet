using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Core.Hosting.AspNetCore.HttpExecutionContext;

/// <summary>
/// Per-request <see cref="ICurrentExecutionContext"/> populated by <see cref="HttpExecutionContextMiddleware"/>.
/// </summary>
public sealed class HttpExecutionContext : ICurrentExecutionContext
{
    public bool IsAvailable { get; private set; }

    public string? UserId { get; private set; }

    public string? TenantId { get; private set; }

    public string? CorrelationId { get; private set; }

    internal void Populate(string? userId, string? tenantId, string? correlationId)
    {
        UserId = userId;
        TenantId = tenantId;
        CorrelationId = correlationId;
        IsAvailable = userId is not null || tenantId is not null || correlationId is not null;
    }
}
