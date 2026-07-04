namespace RaccoonLand.Modules.Persistence.SqlServer.Queries;

/// <summary>
/// Base class for query-side aggregate models. These are plain POCOs (no value objects, plain CLR
/// types) so that queries are easy to write and navigation properties between aggregates are allowed.
/// It carries the useful bits of a domain aggregate root for read scenarios — for example the creation
/// and modification timestamps. The concurrency token is included here so it is present in every output.
/// </summary>
/// <typeparam name="TKey">The primary key type of the aggregate.</typeparam>
public abstract class QueryAggregateRoot<TKey>
{
    public TKey Id { get; set; } = default!;

    /// <summary>Stable business key of the aggregate.</summary>
    public Guid BusinessKey { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset? ModifiedAtUtc { get; set; }

    public string? ModifiedBy { get; set; }

    /// <summary>
    /// Optimistic-concurrency token. Must be present in every query output so callers can echo it back
    /// on the next command.
    /// </summary>
    public Guid ConcurrencyToken { get; set; }
}
