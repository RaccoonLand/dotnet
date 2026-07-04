using System.Text.Json;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;

/// <summary>
/// Persists the domain and service events held by aggregate roots to the outbox table.
/// It runs after EF Core has written the entities but <em>before</em> the surrounding transaction is
/// committed (see <see cref="BaseCommandDbContext"/>), and writes the rows with Dapper on the same
/// connection and transaction, so the events are stored atomically with the aggregate changes.
/// </summary>
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly OutboxOptions _options;

    public OutboxSaveChangesInterceptor(OutboxOptions options) => _options = options;

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        var context = eventData.Context;

        if (context is not null)
        {
            await WriteOutboxAsync(context, cancellationToken);
        }

        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task WriteOutboxAsync(DbContext context, CancellationToken cancellationToken)
    {
        // Runs in SavedChangesAsync, i.e. after EF Core has called AcceptAllChanges: entity states are
        // already reset (Added/Modified -> Unchanged, Deleted -> Detached), so we cannot filter by
        // EntityState here. The raised domain/service events are still held in memory on the aggregates,
        // so we select aggregates that actually carry events.
        var aggregates = context.ChangeTracker
            .Entries<IAggregateRoot>()
            .Select(entry => entry.Entity)
            .Where(aggregate => aggregate.DomainEvents.Count > 0 || aggregate.ServiceEvents.Count > 0)
            .ToList();

        if (aggregates.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        var messages = new List<OutboxMessage>();

        foreach (var aggregate in aggregates)
        {
            var aggregateType = aggregate.GetType().Name;

            // The acting user was already stamped on the aggregate by the audit interceptor (which runs
            // on SavingChanges, before this). Prefer the modifier, falling back to the creator.
            var user = aggregate is IAuditable auditable
                ? auditable.ModifiedBy ?? auditable.CreatedBy
                : null;

            foreach (var domainEvent in aggregate.DomainEvents)
            {
                messages.Add(new OutboxMessage
                {
                    Id = domainEvent.EventId,
                    Category = "Domain",
                    EventType = domainEvent.EventType,
                    AggregateType = aggregateType,
                    AggregateBusinessKey = aggregate.BusinessKey,
                    Payload = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), JsonOptions),
                    CreatedBy = user,
                    OccurredOnUtc = domainEvent.OccurredOnUtc,
                    CreatedOnUtc = now,
                });
            }

            foreach (var serviceEvent in aggregate.ServiceEvents)
            {
                messages.Add(new OutboxMessage
                {
                    Id = serviceEvent.EventId,
                    Category = "Service",
                    EventType = serviceEvent.EventType,
                    AggregateType = aggregateType,
                    AggregateBusinessKey = aggregate.BusinessKey,
                    Payload = JsonSerializer.Serialize(serviceEvent, serviceEvent.GetType(), JsonOptions),
                    CreatedBy = user,
                    OccurredOnUtc = serviceEvent.OccurredOnUtc,
                    CreatedOnUtc = now,
                });
            }
        }

        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            BuildInsertStatement(),
            messages,
            transaction,
            cancellationToken: cancellationToken));

        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
            aggregate.ClearServiceEvents();
        }
    }

    private string BuildInsertStatement() =>
        $"""
         INSERT INTO {_options.QualifiedTableName}
             (Id, Category, EventType, AggregateType, AggregateBusinessKey, Payload, CreatedBy, OccurredOnUtc, CreatedOnUtc)
         VALUES
             (@Id, @Category, @EventType, @AggregateType, @AggregateBusinessKey, @Payload, @CreatedBy, @OccurredOnUtc, @CreatedOnUtc);
         """;
}
