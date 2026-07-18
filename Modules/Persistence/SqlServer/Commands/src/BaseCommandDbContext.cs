using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Events;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands;

/// <summary>
/// Base class for the command-side <see cref="DbContext"/> (the write database).
/// It coordinates a transaction around <see cref="SaveChangesAsync(CancellationToken)"/> so that the
/// outbox interceptors can write with Dapper on the same connection/transaction, keeping the
/// aggregate changes and their outbox rows atomic.
/// </summary>
/// <remarks>
/// <para>
/// By convention all aggregates are saved through <see cref="SaveChangesAsync(CancellationToken)"/>.
/// The synchronous <see cref="SaveChanges()"/> paths are blocked to enforce that convention.
/// </para>
/// <para>
/// When this type owns the transaction, <c>SaveChanges</c> uses <c>acceptAllChangesOnSuccess: false</c> and
/// calls <c>ChangeTracker.AcceptAllChanges()</c> only after a successful <c>CommitAsync</c>, so an
/// execution-strategy retry still sees pending entity changes and in-memory outbox state (events / writer
/// buffer are cleared on <c>TransactionCommitted</c>, not during <c>SavedChangesAsync</c>).
/// </para>
/// <para>
/// Ambiguous commit (database committed but the client never saw success) can still cause a retried attempt.
/// That is a general retry-on-commit limitation; rely on unique outbox/entity keys and idempotent consumers.
/// </para>
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
        => SaveChangesInternalAsync(cancellationToken);

    public override Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        // The owned-transaction path always defers AcceptAllChanges until after commit. When the caller
        // already owns a transaction, honor their acceptAllChangesOnSuccess flag.
        if (Database.CurrentTransaction is not null)
        {
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        return SaveChangesInternalAsync(cancellationToken);
    }

    private async Task<int> SaveChangesInternalAsync(CancellationToken cancellationToken)
    {
        // When the caller already owns a transaction, just save inside it; the outbox interceptor
        // will write within that same transaction and the caller is responsible for committing.
        // In-memory outbox cleanup runs on TransactionCommitted (see outbox interceptors).
        if (Database.CurrentTransaction is not null)
        {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess: true, cancellationToken);
        }

        // Otherwise own the transaction here. The execution strategy makes this safe under
        // SqlServer connection-resiliency (retry-on-failure).
        var strategy = Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            // Explicit per-attempt reset: do not rely solely on TransactionRolledBack after a failed commit.
            OutboxAttemptCleanup.ResetForRetry(this);

            await using var transaction = await Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Outbox interceptors INSERT here; in-memory cleanup waits for TransactionCommitted
                // (and selective event removal by written EventId).
                var affected = await base.SaveChangesAsync(
                    acceptAllChangesOnSuccess: false,
                    cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                ChangeTracker.AcceptAllChanges();

                return affected;
            }
            catch
            {
                OutboxAttemptCleanup.ResetForRetry(this);
                throw;
            }
        });
    }

    public override int SaveChanges() => throw SynchronousSaveNotSupported();

    public override int SaveChanges(bool acceptAllChangesOnSuccess) => throw SynchronousSaveNotSupported();

    private static NotSupportedException SynchronousSaveNotSupported() => new(
        "The command database context must be saved through SaveChangesAsync. " +
        "Synchronous SaveChanges is disabled so the outbox write stays inside the same transaction.");
}
