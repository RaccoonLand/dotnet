using Dapper;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// SQL Server <see cref="IOutboxEventStore"/> that atomically claims unpublished rows
/// (<c>UPDATE … OUTPUT</c> with <c>UPDLOCK, READPAST</c>) and marks them processed via
/// <c>ProcessedOnUtc</c> only when the caller's <see cref="OutboxClaim"/> still matches.
/// Requires <c>ProcessedOnUtc</c> and <c>ClaimedOnUtc</c> columns (see docs).
/// </summary>
public sealed class SqlOutboxEventStore(
    SqlOutboxEventStoreConnectionFactory connectionFactory,
    IOptionsMonitor<OutboxEventStoreOptions> storeOptions) : IOutboxEventStore
{
    private readonly SqlOutboxEventStoreConnectionFactory _connectionFactory = connectionFactory;
    private readonly IOptionsMonitor<OutboxEventStoreOptions> _storeOptions = storeOptions;
    private int _schemaValidated;

    public async Task<IReadOnlyList<OutboxEventRecord>> ClaimPendingAsync(
        int batchSize,
        string? category,
        TimeSpan claimLease,
        CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Batch size must be greater than zero.");
        }

        var leaseSeconds = ClaimLeaseSeconds.ToSqlSeconds(claimLease);

        if (category is not null)
        {
            OutboxEventCategory.EnsureKnown(category, nameof(category));
        }

        var storeOptions = _storeOptions.CurrentValue;
        var table = storeOptions.QualifiedTableName;

        var categoryFilter = category is null
            ? "AND Category IN (@DomainCategory, @ServiceCategory)"
            : "AND Category = @Category";

        var sql = $"""
                   ;WITH candidate AS (
                       SELECT TOP (@BatchSize)
                           Id
                       FROM {table} WITH (UPDLOCK, READPAST, ROWLOCK)
                       WHERE ProcessedOnUtc IS NULL
                         AND (
                             ClaimedOnUtc IS NULL
                             OR ClaimedOnUtc < DATEADD(second, -@LeaseSeconds, SYSUTCDATETIME())
                         )
                         {categoryFilter}
                       ORDER BY CreatedOnUtc, Id
                   )
                   UPDATE target
                   SET ClaimedOnUtc = SYSUTCDATETIME()
                   OUTPUT
                       inserted.Id AS EventId,
                       inserted.Category,
                       inserted.EventType,
                       inserted.AggregateType,
                       inserted.AggregateBusinessKey,
                       inserted.Payload,
                       inserted.CreatedBy,
                       inserted.OccurredOnUtc,
                       inserted.CreatedOnUtc,
                       inserted.ClaimedOnUtc
                   FROM {table} AS target
                   INNER JOIN candidate ON candidate.Id = target.Id;
                   """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await EnsureSchemaOnceAsync(connection, storeOptions, cancellationToken);
        var rows = (await connection.QueryAsync<OutboxEventRecord>(new CommandDefinition(
            sql,
            new
            {
                BatchSize = batchSize,
                Category = category,
                DomainCategory = OutboxEventCategory.Domain,
                ServiceCategory = OutboxEventCategory.Service,
                LeaseSeconds = leaseSeconds,
            },
            cancellationToken: cancellationToken))).ToList();

        return rows;
    }

    public async Task MarkProcessedAsync(
        IReadOnlyCollection<OutboxClaim> claims,
        CancellationToken cancellationToken = default)
    {
        if (claims.Count == 0)
        {
            return;
        }

        var storeOptions = _storeOptions.CurrentValue;
        var table = storeOptions.QualifiedTableName;
        var sql = $"""
                   UPDATE {table}
                   SET ProcessedOnUtc = ISNULL(ProcessedOnUtc, SYSUTCDATETIME())
                   WHERE Id = @EventId
                     AND ClaimedOnUtc = @ClaimedOnUtc;
                   """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await EnsureSchemaOnceAsync(connection, storeOptions, cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var claim in claims)
            {
                var affected = await connection.ExecuteAsync(new CommandDefinition(
                    sql,
                    new { claim.EventId, claim.ClaimedOnUtc },
                    transaction: transaction,
                    cancellationToken: cancellationToken));

                if (affected == 0)
                {
                    throw new InvalidOperationException(
                        $"Failed to mark outbox event {claim.EventId} processed: claim fencing token " +
                        $"(ClaimedOnUtc={claim.ClaimedOnUtc:O}) no longer owns the row.");
                }
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task EnsureSchemaOnceAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        OutboxEventStoreOptions options,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _schemaValidated) == 1)
        {
            return;
        }

        await MessagingSqlSchema.EnsureOutboxAsync(connection, options, cancellationToken);
        Volatile.Write(ref _schemaValidated, 1);
    }
}
