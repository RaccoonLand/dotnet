namespace RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

/// <summary>
/// Base record for paginated query responses.
/// </summary>
/// <typeparam name="TItem">The item type in the page.</typeparam>
public abstract record PageResponse<TItem>
{
    /// <summary>Items in the current page.</summary>
    public IReadOnlyList<TItem> Items { get; init; } = [];

    /// <summary>Total number of matching items, when requested via <see cref="PagedQuery{TResponse}.IncludeTotalCount"/>.</summary>
    public int? TotalCount { get; init; }

    /// <summary>The 1-based page number of this result.</summary>
    public int Page { get; init; }

    /// <summary>The page size used for this result.</summary>
    public int PageSize { get; init; }

    /// <summary>Total number of pages. Zero when <see cref="TotalCount"/> is not available or <see cref="PageSize"/> is not positive.</summary>
    public int TotalPages =>
        !TotalCount.HasValue || PageSize <= 0
            ? 0
            : (int)Math.Ceiling(TotalCount.Value / (double)PageSize);
}
