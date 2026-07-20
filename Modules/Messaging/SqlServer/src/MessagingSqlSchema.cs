using Dapper;
using Microsoft.Data.SqlClient;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// One-shot SQL schema checks so missing Messaging columns/constraints fail before claim/mark work.
/// </summary>
internal static class MessagingSqlSchema
{
    public static async Task EnsureOutboxAsync(
        SqlConnection connection,
        OutboxEventStoreOptions options,
        CancellationToken cancellationToken)
    {
        foreach (var column in new[] { "ClaimedOnUtc", "ProcessedOnUtc" })
        {
            if (!await ColumnExistsAsync(connection, options.Schema, options.Table, column, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Outbox table {options.QualifiedTableName} is missing required column '{column}'. " +
                    "Apply Messaging.SqlServer schema alterations (see docs/2.Schema.md) before using the outbox store.");
            }
        }
    }

    public static async Task EnsureInboxAsync(
        SqlConnection connection,
        InboxStoreOptions options,
        CancellationToken cancellationToken)
    {
        foreach (var column in new[] { "EventId", "EventType", "ClaimedOnUtc", "ProcessedOnUtc", "ReceivedOnUtc" })
        {
            if (!await ColumnExistsAsync(connection, options.Schema, options.Table, column, cancellationToken))
            {
                throw new InvalidOperationException(
                    $"Inbox table {options.QualifiedTableName} is missing required column '{column}'. " +
                    "Create the InboxEvent table (see docs/4.Inbox.md) before using the inbox store.");
            }
        }

        if (!await HasUniqueEventIdAsync(connection, options.Schema, options.Table, cancellationToken))
        {
            throw new InvalidOperationException(
                $"Inbox table {options.QualifiedTableName} must enforce uniqueness on EventId " +
                "(PRIMARY KEY or UNIQUE constraint on EventId alone). Without it, inbox deduplication is incorrect.");
        }
    }

    private static async Task<bool> ColumnExistsAsync(
        SqlConnection connection,
        string schema,
        string table,
        string column,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT CASE WHEN EXISTS (
                               SELECT 1
                               FROM sys.columns AS c
                               INNER JOIN sys.tables AS t ON c.object_id = t.object_id
                               INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
                               WHERE s.name = @Schema
                                 AND t.name = @Table
                                 AND c.name = @Column
                           ) THEN 1 ELSE 0 END;
                           """;

        var result = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { Schema = schema, Table = table, Column = column },
            cancellationToken: cancellationToken));

        return result == 1;
    }

    private static async Task<bool> HasUniqueEventIdAsync(
        SqlConnection connection,
        string schema,
        string table,
        CancellationToken cancellationToken)
    {
        const string sql = """
                           SELECT CASE WHEN EXISTS (
                               SELECT 1
                               FROM sys.indexes AS i
                               INNER JOIN sys.index_columns AS ic
                                   ON i.object_id = ic.object_id AND i.index_id = ic.index_id
                               INNER JOIN sys.columns AS c
                                   ON ic.object_id = c.object_id AND ic.column_id = c.column_id
                               INNER JOIN sys.tables AS t ON i.object_id = t.object_id
                               INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
                               WHERE s.name = @Schema
                                 AND t.name = @Table
                                 AND c.name = N'EventId'
                                 AND ic.key_ordinal = 1
                                 AND ic.is_included_column = 0
                                 AND (i.is_primary_key = 1 OR i.is_unique = 1 OR i.is_unique_constraint = 1)
                                 AND NOT EXISTS (
                                     SELECT 1
                                     FROM sys.index_columns AS ic2
                                     WHERE ic2.object_id = i.object_id
                                       AND ic2.index_id = i.index_id
                                       AND ic2.key_ordinal > 1
                                       AND ic2.is_included_column = 0)
                           ) THEN 1 ELSE 0 END;
                           """;

        var result = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            sql,
            new { Schema = schema, Table = table },
            cancellationToken: cancellationToken));

        return result == 1;
    }
}
