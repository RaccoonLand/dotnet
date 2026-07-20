namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// SQL Server-specific options for <see cref="SqlOutboxEventStore"/>.
/// Bound from the same root section as <see cref="RaccoonLand.Modules.Messaging.Abstractions.OutboxEventStoreOptions"/>
/// (<c>OutboxEventStore</c>) unless overridden at registration.
/// Claim lease duration is owned by <c>OutboxRelayOptions.ClaimLease</c> and passed into
/// <see cref="RaccoonLand.Modules.Messaging.Abstractions.IOutboxEventStore.ClaimPendingAsync"/>.
/// </summary>
public sealed class SqlOutboxEventStoreOptions
{
    /// <summary>SQL Server connection string used by the event store (command database or outbox database).</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
