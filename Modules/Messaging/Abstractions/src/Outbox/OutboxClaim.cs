namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Fencing token for a live outbox claim. <see cref="ClaimedOnUtc"/> must match the value set when
/// the row was claimed so a stale worker cannot mark a row that another worker has reclaimed.
/// </summary>
public readonly record struct OutboxClaim(Guid EventId, DateTimeOffset ClaimedOnUtc);
