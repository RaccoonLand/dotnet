using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace RaccoonLand.Modules.Messaging.SqlServer;

/// <summary>
/// Creates open <see cref="SqlConnection"/> instances for the inbox store.
/// </summary>
public sealed class SqlInboxStoreConnectionFactory(IOptionsMonitor<SqlInboxStoreOptions> options)
{
    private readonly IOptionsMonitor<SqlInboxStoreOptions> _options = options;

    public async Task<SqlConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connectionString = _options.CurrentValue.ConnectionString;
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "SqlInboxStoreOptions.ConnectionString is required. " +
                "Configure it under section 'InboxStore' or via AddRaccoonLandInboxStore.");
        }

        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
