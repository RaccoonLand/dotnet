using Microsoft.EntityFrameworkCore;

namespace RaccoonLand.Modules.Persistence.SqlServer.Queries;

/// <summary>
/// Base class for the query-side <see cref="DbContext"/> (the read database).
/// Rules enforced here:
///  1) The context is read-only: any SaveChanges call throws to alert the developer.
///  2) Change tracking is turned off by default (no query is tracked) for better performance.
/// </summary>
public abstract class BaseQueryDbContext : DbContext
{
    protected BaseQueryDbContext(DbContextOptions options) : base(options)
    {
        ChangeTracker.QueryTrackingBehavior = QueryTrackingBehavior.NoTracking;
        ChangeTracker.AutoDetectChangesEnabled = false;
    }

    public override int SaveChanges() => throw ReadOnly();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw ReadOnly();

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => throw ReadOnly();

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
        => throw ReadOnly();

    private static InvalidOperationException ReadOnly() => new(
        "The query database context is read-only and cannot persist changes. " +
        "Use a command database context (BaseCommandDbContext) for writes.");
}
