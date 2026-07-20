namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Storage-agnostic access to the aggregate-event outbox: claim a batch of unpublished rows and
/// mark them processed after successful dispatch/publish. Implementations must support concurrent
/// workers (for example SQL <c>UPDLOCK, READPAST</c>) and honour claim-lease /
/// <see cref="OutboxClaim"/> fencing.
/// </summary>
public interface IOutboxEventStore
{
    /// <summary>
    /// Atomically claims up to <paramref name="batchSize"/> unpublished rows whose claim lease has
    /// expired (or was never set), optionally filtered by <paramref name="category"/>. Claimed rows
    /// remain unpublished until <see cref="MarkProcessedAsync"/> with a matching
    /// <see cref="OutboxClaim"/>; a failed worker simply leaves them for the next poll after
    /// <paramref name="claimLease"/>.
    /// </summary>
    /// <param name="batchSize">Maximum rows to claim; must be greater than zero.</param>
    /// <param name="category">
    /// When non-null, must be a known <see cref="OutboxEventCategory"/> value. When null, only known
    /// categories are eligible (unknown category rows stay pending for operational cleanup).
    /// </param>
    /// <param name="claimLease">Exclusive claim duration before another worker may reclaim.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyList<OutboxEventRecord>> ClaimPendingAsync(
        int batchSize,
        string? category,
        TimeSpan claimLease,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the given claims as processed when each row still holds the same
    /// <see cref="OutboxClaim.ClaimedOnUtc"/>. Idempotent when already processed with the same claim;
    /// must not succeed for a stale claim after reclaim.
    /// </summary>
    Task MarkProcessedAsync(
        IReadOnlyCollection<OutboxClaim> claims,
        CancellationToken cancellationToken = default);
}
