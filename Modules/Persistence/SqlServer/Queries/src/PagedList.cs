namespace RaccoonLand.Modules.Persistence.SqlServer.Queries;

/// <summary>
/// The result of executing a paginated EF Core query.
/// Map to a CQRS <c>PageResponse&lt;TItem&gt;</c> in the endpoint layer.
/// </summary>
/// <typeparam name="T">The item type in the page.</typeparam>
public sealed record PagedList<T>
{
    /// <summary>Items in the current page.</summary>
    public IReadOnlyList<T> Items { get; init; } = [];

    /// <summary>Total matching items when requested; otherwise <see langword="null"/>.</summary>
    public int? TotalCount { get; init; }

    /// <summary>The effective 1-based page number.</summary>
    public int Page { get; init; }

    /// <summary>The effective page size.</summary>
    public int PageSize { get; init; }
}
