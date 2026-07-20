namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Result of attempting to claim a service event in the consumer inbox (idempotency gate).
/// </summary>
public enum InboxClaimResult
{
    /// <summary>This consumer may process the event (new claim or reclaimed expired lease).</summary>
    Claimed = 0,

    /// <summary>The event was already processed successfully; acknowledge and skip handlers.</summary>
    AlreadyProcessed = 1,

    /// <summary>Another worker holds a live claim; requeue without running handlers.</summary>
    ClaimHeldByOther = 2,
}
