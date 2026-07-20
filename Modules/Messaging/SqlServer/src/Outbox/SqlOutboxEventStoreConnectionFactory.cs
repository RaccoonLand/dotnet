using Microsoft.Data.SqlClient;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// Creates open <see cref="SqlConnection"/> instances for the outbox event store.
/// The host supplies the connection string via <see cref="SqlOutboxEventStoreOptions"/>.
/// </summary>
public sealed class SqlOutboxEventStoreConnectionFactory(Microsoft.Extensions.Options.IOptionsMonitor<SqlOutboxEventStoreOptions> options)
{
    private readonly Microsoft.Extensions.Options.IOptionsMonitor<SqlOutboxEventStoreOptions> _options = options;

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _options.CurrentValue.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SqlOutboxEventStoreOptions.ConnectionString is required. " +
                "Configure it under section 'OutboxEventStore' or via AddRaccoonLandOutboxEventStore.");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
