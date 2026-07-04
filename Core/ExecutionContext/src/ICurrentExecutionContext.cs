namespace RaccoonLand.Core.ExecutionContext.Abstractions;

/// <summary>
/// Provides information about the current execution scope.
/// The execution scope may originate from an HTTP request,
/// a message consumer, a background job, or any other host.
/// </summary>
public interface ICurrentExecutionContext
{
    /// <summary>Indicates whether execution context information is available.</summary>
    bool IsAvailable { get; }

    /// <summary>The current user identifier.</summary>
    string? UserId { get; }

    /// <summary>The current tenant identifier.</summary>
    string? TenantId { get; }

    /// <summary>Correlation identifier used for tracing.</summary>
    string? CorrelationId { get; }
}
