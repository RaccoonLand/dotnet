using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands;

/// <summary>
/// Base class for the command-side <see cref="DbContext"/> (the write database).
/// It coordinates a transaction around <see cref="SaveChangesAsync(CancellationToken)"/> so that the
/// outbox interceptor can write events with Dapper on the same connection/transaction, keeping the
/// aggregate changes and their events atomic.
/// </summary>
/// <remarks>
/// By convention all aggregates are saved through <see cref="SaveChangesAsync(CancellationToken)"/>.
/// The synchronous <see cref="SaveChanges()"/> paths are blocked to enforce that convention.
/// </remarks>
public abstract class BaseCommandDbContext : DbContext
{
    protected BaseCommandDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);
        ExcludeEventsFromModel(modelBuilder);
    }

    /// <summary>
    /// Removes in-memory domain/service events from the EF model. They are persisted through the outbox
    /// interceptor, not as relational entities.
    /// </summary>
    private static void ExcludeEventsFromModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.Ignore<ServiceEvent>();

        foreach (var entityType in modelBuilder.Model.GetEntityTypes().ToList())
        {
            var clrType = entityType.ClrType;

            if (typeof(DomainEvent).IsAssignableFrom(clrType) || typeof(ServiceEvent).IsAssignableFrom(clrType))
            {
                modelBuilder.Ignore(clrType);
                continue;
            }

            if (!typeof(IAggregateRoot).IsAssignableFrom(clrType))
            {
                continue;
            }

            modelBuilder.Entity(clrType).Ignore(nameof(IAggregateRoot.DomainEvents));
            modelBuilder.Entity(clrType).Ignore(nameof(IAggregateRoot.ServiceEvents));
        }
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => SaveChangesInternalAsync(acceptAllChangesOnSuccess: true, cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
        => SaveChangesInternalAsync(acceptAllChangesOnSuccess, cancellationToken);

    private async Task<int> SaveChangesInternalAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken)
    {
        // When the caller already owns a transaction, just save inside it; the outbox interceptor
        // will write within that same transaction and the caller is responsible for committing.
        if (Database.CurrentTransaction is not null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        // Otherwise own the transaction here. The execution strategy makes this safe under
        // SqlServer connection-resiliency (retry-on-failure).
        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);

            // The outbox interceptor's SavedChangesAsync runs here, before the commit below.
            var affected = await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return affected;
        });
    }

    public override int SaveChanges() => throw SynchronousSaveNotSupported();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw SynchronousSaveNotSupported();

    private static NotSupportedException SynchronousSaveNotSupported() => new(
        "The command database context must be saved through SaveChangesAsync. " +
        "Synchronous SaveChanges is disabled so the outbox write stays inside the same transaction.");
}
