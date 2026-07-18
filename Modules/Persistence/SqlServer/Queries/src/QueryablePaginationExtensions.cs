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
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="maxPageSize"/> is less than 1.
    /// </exception>
    public static PageRequest Normalize(
        int page,
        int pageSize,
        bool includeTotalCount,
        int maxPageSize = 100)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(maxPageSize, 1);

        return new PageRequest(
            Math.Max(page, 1),
            Math.Clamp(pageSize, 1, maxPageSize),
            includeTotalCount);
    }

    /// <summary>
    /// Applies 1-based <c>Skip</c>/<c>Take</c> paging to the query.
    /// Expects a normalized <see cref="PageRequest"/> (for example from <see cref="Normalize"/> or
    /// <see cref="ToPagedListAsync{T}"/>); invalid page/size values throw.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="request"/> has <c>Page</c> or <c>PageSize</c> less than 1, or the skip offset overflows
    /// <see cref="int"/>.
    /// </exception>
    public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PageRequest request)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (request.Page < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.Page,
                $"{nameof(PageRequest.Page)} must be at least 1. Use {nameof(Normalize)} or {nameof(ToPagedListAsync)}.");
        }

        if (request.PageSize < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request.PageSize,
                $"{nameof(PageRequest.PageSize)} must be at least 1. Use {nameof(Normalize)} or {nameof(ToPagedListAsync)}.");
        }

        int skip;
        try
        {
            checked
            {
                skip = (request.Page - 1) * request.PageSize;
            }
        }
        catch (OverflowException exception)
        {
            throw new ArgumentOutOfRangeException(
                nameof(request),
                request,
                $"{nameof(PageRequest.Page)} and {nameof(PageRequest.PageSize)} produce a Skip offset that overflows Int32. ({exception.Message})");
        }

        return query
            .Skip(skip)
            .Take(request.PageSize);
    }

    /// <summary>
    /// Executes a paginated query. Apply filtering, stable ordering, and projection before calling this method.
    /// When <paramref name="includeTotalCount"/> is <see langword="true"/>, counts the filtered query
    /// before fetching the page (two round-trips; the count and page are not a single snapshot).
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
