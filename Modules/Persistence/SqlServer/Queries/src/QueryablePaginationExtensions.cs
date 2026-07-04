using Microsoft.EntityFrameworkCore;

namespace RaccoonLand.Modules.Persistence.SqlServer.Queries;

/// <summary>
/// Pagination helpers for read-side <see cref="IQueryable{T}"/> queries.
/// </summary>
public static class QueryablePaginationExtensions
{
    /// <summary>
    /// Normalizes pagination input: page is at least 1 and page size is clamped to
    /// <c>[1, maxPageSize]</c>.
    /// </summary>
    public static PageRequest Normalize(
        int page,
        int pageSize,
        bool includeTotalCount,
        int maxPageSize = 100)
    {
        return new PageRequest(
            Math.Max(page, 1),
            Math.Clamp(pageSize, 1, maxPageSize),
            includeTotalCount);
    }

    /// <summary>
    /// Applies 1-based <c>Skip</c>/<c>Take</c> paging to the query.
    /// </summary>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize);
    }

    /// <summary>
    /// Executes a paginated query. Apply filtering, ordering, and projection before calling this method.
    /// When <paramref name="includeTotalCount"/> is <see langword="true"/>, counts the filtered query
    /// before fetching the page.
    /// </summary>
    public static async Task<PagedList<T>> ToPagedListAsync<T>(
        this IQueryable<T> query,
        int page,
        int pageSize,
        bool includeTotalCount = false,
        int maxPageSize = 100,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var request = Normalize(page, pageSize, includeTotalCount, maxPageSize);

        int? totalCount = null;
        if (request.IncludeTotalCount)
        {
            totalCount = await query.CountAsync(cancellationToken);
        }

        var items = await query
            .ApplyPaging(request)
            .ToListAsync(cancellationToken);

        return new PagedList<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize,
        };
    }
}
