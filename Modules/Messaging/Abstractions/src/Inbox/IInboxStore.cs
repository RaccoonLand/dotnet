namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Storage-agnostic inbox for cross-service consumers. Ensures each <c>EventId</c> is processed
/// at most once per consumer service (at-least-once transport + exactly-once handling intent).
/// Completion and release require the <see cref="InboxClaimToken"/> from a successful claim.
/// </summary>
public interface IInboxStore
{
    /// <summary>
    /// Tries to claim <paramref name="eventId"/> for processing. Uses a claim lease so crashed
    /// workers can be reclaimed after <paramref name="claimLease"/>.
    /// </summary>
    Task<InboxClaimAttempt> TryClaimAsync(
        Guid eventId,
        string eventType,
        TimeSpan claimLease,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the event as successfully processed when the caller still owns
    /// <paramref name="claim"/>. Idempotent when already processed under the same claim stamp.
    /// </summary>
    Task MarkProcessedAsync(InboxClaimToken claim, CancellationToken cancellationToken = default);

    /// <summary>
    /// Releases a live claim after a handler failure so another delivery can reclaim, but only when
    /// <paramref name="claim"/> still matches the row. When <paramref name="clearClaimImmediately"/>
    /// is false, this is a no-op (lease expiry handles reclaim).
    /// </summary>
    Task ReleaseAsync(
        InboxClaimToken claim,
        bool clearClaimImmediately = true,
        CancellationToken cancellationToken = default);
}
