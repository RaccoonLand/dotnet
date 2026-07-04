using Dapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using RaccoonLand.Modules.Persistence.Outbox.Abstraction;
using RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Interceptors;

/// <summary>
/// Drains the request-scoped <see cref="IOutboxWriter"/> buffer and persists messages to their registered
/// outbox channel tables with Dapper on the same connection and transaction as <c>SaveChanges</c>.
/// This is distinct from <see cref="OutboxSaveChangesInterceptor"/>, which writes aggregate domain/service
/// events; this one writes messages explicitly enqueued through <see cref="IOutboxWriter"/>.
/// </summary>
public sealed class OutboxWriterSaveChangesInterceptor(
    IOutboxChannelRegistry registry,
    OutboxWriter writer) : SaveChangesInterceptor
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

    private async Task WriteOutboxAsync(DbContext context, CancellationToken cancellationToken)
    {
        var batches = _writer.Drain();
        if (batches.Count == 0)
        {
            return;
        }

        var connection = context.Database.GetDbConnection();
        var transaction = context.Database.CurrentTransaction?.GetDbTransaction();
        var createdOnUtc = DateTimeOffset.UtcNow;

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
