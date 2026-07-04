namespace RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

/// <summary>
/// Base class for paginated queries. Carries page parameters and implements <see cref="IQuery{TResponse}"/>.
/// </summary>
/// <typeparam name="TResponse">The paginated response type.</typeparam>
public abstract class PagedQuery<TResponse> : IQuery<TResponse>
{
    /// <summary>1-based page number. Default is 1.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Number of items per page. Default is 20.</summary>
    public int PageSize { get; init; } = 20;

    /// <summary>When <see langword="true"/>, the handler should populate <see cref="PageResponse{TItem}.TotalCount"/>.</summary>
    public bool IncludeTotalCount { get; init; }
}
