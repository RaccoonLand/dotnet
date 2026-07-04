using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Data;

/// <summary>
/// Dapper-based access to the two configurable authorization stored procedures. Each call returns a set of
/// request full-names; the provider checks membership in memory.
/// </summary>
public sealed class SqlAuthorizationRepository(IOptions<SqlAuthorizationOptions> options)
{
    private readonly SqlAuthorizationOptions _options = options.Value;

    /// <summary>Executes the anonymous-requests procedure and returns the distinct request names.</summary>
    public async Task<IReadOnlyCollection<string>> GetAnonymousRequestsAsync(CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);

        var names = await connection.QueryAsync<string?>(new CommandDefinition(
            _options.AnonymousRequestsProcedure,
            commandType: CommandType.StoredProcedure,
            commandTimeout: _options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken));

        return ToSet(names);
    }

    /// <summary>
    /// Executes the allowed-requests procedure for <paramref name="userId"/> and returns the distinct request
    /// names the user may execute.
    /// </summary>
    public async Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.ConnectionString);

        var parameters = new DynamicParameters();
        parameters.Add(_options.UserIdParameterName, userId);

        var names = await connection.QueryAsync<string?>(new CommandDefinition(
            _options.AllowedRequestsProcedure,
            parameters,
            commandType: CommandType.StoredProcedure,
            commandTimeout: _options.CommandTimeoutSeconds,
            cancellationToken: cancellationToken));

        return ToSet(names);
    }

    private static HashSet<string> ToSet(IEnumerable<string?> names)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);

        foreach (var name in names)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                set.Add(name);
            }
        }

        return set;
    }
}
