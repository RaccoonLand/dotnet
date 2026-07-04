namespace RaccoonLand.Core.ExecutionContext.Abstractions;

/// <summary>
/// Default implementation used when no execution context exists.
/// </summary>
public sealed class NullCurrentExecutionContext : ICurrentExecutionContext
{
    public static NullCurrentExecutionContext Instance { get; } = new();

    private NullCurrentExecutionContext()
    {
    }

    public bool IsAvailable => false;

    public string? UserId => null;

    public string? TenantId => null;

    public string? CorrelationId => null;
}
