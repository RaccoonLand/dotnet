namespace RaccoonLand.Core.Hosting.Worker;

/// <summary>Optional execution metadata applied to a single background job before dispatch.</summary>
public sealed record WorkerExecutionMetadata
{
    public string? UserId { get; init; }

    public string? TenantId { get; init; }

    public string? CorrelationId { get; init; }
}
