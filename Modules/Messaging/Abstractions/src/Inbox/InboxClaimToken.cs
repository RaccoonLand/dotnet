namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Fencing token for a live inbox claim. <see cref="ClaimedOnUtc"/> must match the value written by
/// <see cref="IInboxStore.TryClaimAsync"/> so a stale worker cannot mark or release a claim held by
/// another consumer after lease reclaim.
/// </summary>
public readonly record struct InboxClaimToken(Guid EventId, DateTimeOffset ClaimedOnUtc);
