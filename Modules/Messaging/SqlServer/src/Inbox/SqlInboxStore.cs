using Dapper;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Messaging.Abstractions;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// SQL Server <see cref="IInboxStore"/> using a unique <c>EventId</c> primary key plus claim/processed
/// timestamps. Mark/release require a matching <see cref="InboxClaimToken"/>.
/// Claims run in a single explicit transaction so concurrent workers cannot both observe
/// <see cref="InboxClaimResult.Claimed"/> for the same <c>EventId</c>.
/// </summary>
public sealed class SqlInboxStore(
    SqlInboxStoreConnectionFactory connectionFactory,
    IOptionsMonitor<InboxStoreOptions> storeOptions) : IInboxStore
{
    private readonly SqlInboxStoreConnectionFactory _connectionFactory = connectionFactory;
    private readonly IOptionsMonitor<InboxStoreOptions> _storeOptions = storeOptions;
    private int _schemaValidated;

    public async Task<InboxClaimAttempt> TryClaimAsync(
        Guid eventId,
        string eventType,
        TimeSpan claimLease,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        var leaseSeconds = ClaimLeaseSeconds.ToSqlSeconds(claimLease);

        var storeOptions = _storeOptions.CurrentValue;
        var table = storeOptions.QualifiedTableName;

        // Single transaction: UPDLOCK/HOLDLOCK held until COMMIT so claim decisions are exclusive.
        // ResultCode: 0 Claimed, 1 AlreadyProcessed, 2 ClaimHeldByOther, 3 EventTypeMismatch
        var sql = $"""
                   SET XACT_ABORT ON;
                   BEGIN TRAN;

                   DECLARE @now datetimeoffset = SYSUTCDATETIME();
                   DECLARE @processed datetimeoffset;
                   DECLARE @claimedOn datetimeoffset;
                   DECLARE @existingType nvarchar(256);
                   DECLARE @result int;
                   DECLARE @claimed datetimeoffset = NULL;

                   SELECT
                       @processed = ProcessedOnUtc,
                       @claimedOn = ClaimedOnUtc,
                       @existingType = EventType
                   FROM {table} WITH (UPDLOCK, HOLDLOCK, ROWLOCK)
                   WHERE EventId = @EventId;

                   IF @processed IS NOT NULL
                       SET @result = 1;
                   ELSE IF @claimedOn IS NOT NULL
                            AND @claimedOn >= DATEADD(second, -@LeaseSeconds, @now)
                       SET @result = 2;
                   ELSE IF @existingType IS NOT NULL AND @existingType <> @EventType
                       SET @result = 3;
                   ELSE
                   BEGIN
                       MERGE {table} WITH (HOLDLOCK) AS target
                       USING (SELECT @EventId AS EventId) AS source
                       ON target.EventId = source.EventId
                       WHEN MATCHED AND target.ProcessedOnUtc IS NULL THEN
                           UPDATE SET ClaimedOnUtc = @now
                       WHEN NOT MATCHED THEN
                           INSERT (EventId, EventType, ClaimedOnUtc, ProcessedOnUtc, ReceivedOnUtc)
                           VALUES (@EventId, @EventType, @now, NULL, @now);

                       IF @@ROWCOUNT = 0
                           SET @result = 1;
                       ELSE
                       BEGIN
                           SET @result = 0;
                           SET @claimed = @now;
                       END
                   END

                   SELECT @result AS ResultCode, @claimed AS ClaimedOnUtc;
                   COMMIT TRAN;
                   """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await EnsureSchemaOnceAsync(connection, storeOptions, cancellationToken);
        var row = await connection.QuerySingleAsync<ClaimRow>(new CommandDefinition(
            sql,
            new { EventId = eventId, EventType = eventType, LeaseSeconds = leaseSeconds },
            cancellationToken: cancellationToken));

        return row.ResultCode switch
        {
            0 when row.ClaimedOnUtc is { } claimedOnUtc => InboxClaimAttempt.Claimed(new InboxClaimToken(eventId, claimedOnUtc)),
            0 => throw new InvalidOperationException("Inbox claim succeeded without ClaimedOnUtc."),
            1 => InboxClaimAttempt.AlreadyProcessed(),
            2 => InboxClaimAttempt.ClaimHeldByOther(),
            3 => throw new InvalidOperationException(
                $"Inbox EventId {eventId} was previously recorded with a different EventType than '{eventType}'. " +
                "EventType is immutable after the first insert."),
            _ => throw new InvalidOperationException($"Unexpected inbox claim result code {row.ResultCode}."),
        };
    }

    public async Task MarkProcessedAsync(InboxClaimToken claim, CancellationToken cancellationToken = default)
    {
        var storeOptions = _storeOptions.CurrentValue;
        var table = storeOptions.QualifiedTableName;
        var sql = $"""
                   UPDATE {table}
                   SET ProcessedOnUtc = ISNULL(ProcessedOnUtc, SYSUTCDATETIME())
                   WHERE EventId = @EventId
                     AND ClaimedOnUtc = @ClaimedOnUtc;
                   """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await EnsureSchemaOnceAsync(connection, storeOptions, cancellationToken);
        var affected = await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { claim.EventId, claim.ClaimedOnUtc },
            cancellationToken: cancellationToken));

        if (affected == 0)
        {
            throw new InvalidOperationException(
                $"Failed to mark inbox event {claim.EventId} processed: claim fencing token " +
                $"(ClaimedOnUtc={claim.ClaimedOnUtc:O}) no longer owns the row.");
        }
    }

    public async Task ReleaseAsync(
        InboxClaimToken claim,
        bool clearClaimImmediately = true,
        CancellationToken cancellationToken = default)
    {
        if (!clearClaimImmediately)
        {
            return;
        }

        var storeOptions = _storeOptions.CurrentValue;
        var table = storeOptions.QualifiedTableName;
        var sql = $"""
                   UPDATE {table}
                   SET ClaimedOnUtc = NULL
                   WHERE EventId = @EventId
                     AND ClaimedOnUtc = @ClaimedOnUtc
                     AND ProcessedOnUtc IS NULL;
                   """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await EnsureSchemaOnceAsync(connection, storeOptions, cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(
            sql,
            new { claim.EventId, claim.ClaimedOnUtc },
            cancellationToken: cancellationToken));
    }

    private async Task EnsureSchemaOnceAsync(
        Microsoft.Data.SqlClient.SqlConnection connection,
        InboxStoreOptions options,
        CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _schemaValidated) == 1)
        {
            return;
        }

        await MessagingSqlSchema.EnsureInboxAsync(connection, options, cancellationToken);
        Volatile.Write(ref _schemaValidated, 1);
    }

    private sealed class ClaimRow
    {
        public int ResultCode { get; init; }

        public DateTimeOffset? ClaimedOnUtc { get; init; }
    }
}
