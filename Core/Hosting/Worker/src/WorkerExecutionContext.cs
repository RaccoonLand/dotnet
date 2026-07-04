using RaccoonLand.Core.ExecutionContext.Abstractions;

namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>
/// Per-scope execution context for background jobs. Configured by
/// <see cref="WorkerRequestDispatcher"/> before each dispatch.
/// </summary>
public sealed class WorkerExecutionContext : ICurrentExecutionContext
{
    public bool IsAvailable { get; private set; }

    public string? UserId { get; private set; }

    public string? TenantId { get; private set; }

    public string? CorrelationId { get; private set; }

    internal void Configure(WorkerExecutionMetadata? metadata)
    {
        if (metadata is null)
        {
            IsAvailable = false;
            UserId = null;
            TenantId = null;
            CorrelationId = null;
            return;
        }

        UserId = metadata.UserId;
        TenantId = metadata.TenantId;
        CorrelationId = metadata.CorrelationId;
        IsAvailable = UserId is not null || TenantId is not null || CorrelationId is not null;
    }
}
