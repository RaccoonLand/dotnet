using System.Data.Common;
using System.Runtime.CompilerServices;
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
/// Writes on <see cref="SavedChangesAsync"/>; removes only the written event ids on
/// <see cref="TransactionCommittedAsync"/>. Per-<see cref="DbContext"/> flush state is stored in a
/// <see cref="ConditionalWeakTable{TKey,TValue}"/> so the interceptor instance remains safe when shared.
/// </summary>
public sealed class OutboxSaveChangesInterceptor : SaveChangesInterceptor, IDbTransactionInterceptor
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly ConditionalWeakTable<DbContext, FlushState> States = new();

    private readonly OutboxOptions _options;

    public OutboxSaveChangesInterceptor(OutboxOptions options) => _options = options;

    /// <summary>
    /// Clears per-context flush marks and does not remove events from aggregates. Used at the start of an
    /// execution-strategy attempt and after a failed attempt when <c>TransactionRolledBack</c> may not run.
    /// </summary>
    internal static void ResetAttemptState(DbContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (States.TryGetValue(context, out var state))
        {
            state.Reset();
        }
    }

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

    public Task TransactionCommittedAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        CommitFlush(eventData.Context);
        return Task.CompletedTask;
    }

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        => CommitFlush(eventData.Context);

    public Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            ResetAttemptState(eventData.Context);
        }

        return Task.CompletedTask;
    }

    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
    {
        if (eventData.Context is not null)
        {
            ResetAttemptState(eventData.Context);
        }
    }

    private async Task WriteOutboxAsync(DbContext context, CancellationToken cancellationToken)
    {
        var state = States.GetValue(context, static _ => new FlushState());

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

            var user = aggregate is IAuditable auditable
                ? auditable.ModifiedBy ?? auditable.CreatedBy
                : null;

            foreach (var domainEvent in aggregate.DomainEvents)
            {
                if (state.WrittenEventIds.Contains(domainEvent.EventId))
                {
                    continue;
                }

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
                if (state.WrittenEventIds.Contains(serviceEvent.EventId))
                {
                    continue;
                }

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

        if (messages.Count == 0)
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();

        await connection.ExecuteAsync(new CommandDefinition(
            BuildInsertStatement(),
            messages,
            transaction,
            cancellationToken: cancellationToken));

        var writtenIds = messages.Select(message => message.Id).ToHashSet();

        if (context.Database.CurrentTransaction is null)
        {
            foreach (var aggregate in aggregates)
            {
                aggregate.RemoveDomainEvents(writtenIds);
                aggregate.RemoveServiceEvents(writtenIds);
            }
        }
        else
        {
            foreach (var id in writtenIds)
            {
                state.WrittenEventIds.Add(id);
            }

            foreach (var aggregate in aggregates)
            {
                state.Aggregates.Add(aggregate);
            }
        }
    }

    private static void CommitFlush(DbContext? context)
    {
        if (context is null || !States.TryGetValue(context, out var state))
        {
            return;
        }

        if (state.WrittenEventIds.Count > 0)
        {
            var writtenIds = state.WrittenEventIds;
            foreach (var aggregate in state.Aggregates)
            {
                aggregate.RemoveDomainEvents(writtenIds);
                aggregate.RemoveServiceEvents(writtenIds);
            }
        }

        state.Reset();
    }

    private string BuildInsertStatement() =>
        $"""
         INSERT INTO {_options.QualifiedTableName}
             (Id, Category, EventType, AggregateType, AggregateBusinessKey, Payload, CreatedBy, OccurredOnUtc, CreatedOnUtc)
         VALUES
             (@Id, @Category, @EventType, @AggregateType, @AggregateBusinessKey, @Payload, @CreatedBy, @OccurredOnUtc, @CreatedOnUtc);
         """;

    private sealed class FlushState
    {
        public HashSet<Guid> WrittenEventIds { get; } = [];
        public HashSet<IAggregateRoot> Aggregates { get; } = [];

        public void Reset()
        {
            WrittenEventIds.Clear();
            Aggregates.Clear();
        }
    }
}
