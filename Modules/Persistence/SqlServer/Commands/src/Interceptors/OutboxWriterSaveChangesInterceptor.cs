using System.Data.Common;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;

/// <summary>
/// Flushes the request-scoped <see cref="IOutboxWriter"/> buffer to registered channel tables with Dapper on
/// the same connection and transaction as <c>SaveChanges</c>. Buffered messages are removed only after
/// <see cref="TransactionCommittedAsync"/> (or immediately when no ambient transaction exists) so a failed
/// commit can retry without losing enqueued messages.
/// </summary>
public sealed class OutboxWriterSaveChangesInterceptor(
    IOutboxChannelRegistry registry,
    OutboxWriter writer) : SaveChangesInterceptor, IDbTransactionInterceptor
{
    private readonly IOutboxChannelRegistry _registry = registry;
    private readonly OutboxWriter _writer = writer;

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
        _writer.ClearFlushedOnCommit();
        return Task.CompletedTask;
    }

    public void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData)
        => _writer.ClearFlushedOnCommit();

    public Task TransactionRolledBackAsync(
        DbTransaction transaction,
        TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        _writer.RestoreFlushedOnRollback();
        return Task.CompletedTask;
    }

    public void TransactionRolledBack(DbTransaction transaction, TransactionEndEventData eventData)
        => _writer.RestoreFlushedOnRollback();

    private async Task WriteOutboxAsync(DbContext context, CancellationToken cancellationToken)
    {
        var batches = _writer.GetPendingBatches();
        if (batches.Count == 0)
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        var createdOnUtc = DateTimeOffset.UtcNow;
        var flushedIds = new List<Guid>();

        foreach (var batch in batches)
        {
            var options = _registry.Get(batch.ChannelType)
                ?? throw new InvalidOperationException(
                    $"Outbox channel '{batch.ChannelType.FullName}' is not registered.");

            var rows = batch.Messages
                .Select(message => new OutboxRow
                {
                    Id = message.Id,
                    EventType = message.EventType,
                    AggregateType = message.AggregateType,
                    AggregateBusinessKey = message.AggregateBusinessKey,
                    Payload = message.Payload,
                    CreatedBy = message.CreatedBy,
                    OccurredOnUtc = message.OccurredOnUtc,
                    CreatedOnUtc = createdOnUtc,
                })
                .ToList();

            await connection.ExecuteAsync(new CommandDefinition(
                BuildInsertStatement(options.QualifiedTableName),
                rows,
                transaction,
                cancellationToken: cancellationToken));

            flushedIds.AddRange(batch.Messages.Select(message => message.Id));
        }

        if (context.Database.CurrentTransaction is null)
        {
            _writer.ClearPending();
        }
        else
        {
            _writer.MarkFlushed(flushedIds);
        }
    }

    private static string BuildInsertStatement(string qualifiedTableName) =>
        $"""
         INSERT INTO {qualifiedTableName}
             (Id, EventType, AggregateType, AggregateBusinessKey, Payload, CreatedBy, OccurredOnUtc, CreatedOnUtc)
         VALUES
             (@Id, @EventType, @AggregateType, @AggregateBusinessKey, @Payload, @CreatedBy, @OccurredOnUtc, @CreatedOnUtc);
         """;
}
