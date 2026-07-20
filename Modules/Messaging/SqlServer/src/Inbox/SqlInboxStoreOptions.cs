namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// SQL Server-specific options for <see cref="SqlInboxStore"/>.
/// Bound from the same root section as <see cref="RaccoonLand.Modules.Messaging.Abstractions.InboxStoreOptions"/>
/// (<c>InboxStore</c>) unless overridden at registration.
/// Claim lease is supplied by the consumer on each <c>TryClaimAsync</c> call.
/// </summary>
public sealed class SqlInboxStoreOptions
{
    /// <summary>SQL Server connection string used by the inbox store.</summary>
    public string ConnectionString { get; set; } = string.Empty;
}
