namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Data;

/// <summary>
/// Data access for the SQL Server authorization provider: resolves the anonymous request set and the
/// per-user allowed request set from the configured stored procedures. Each call returns a set of request
/// full-names; the provider checks membership in memory. Connection, timeout, and SQL failures throw; they are
/// not converted to an authorization deny.
/// </summary>
public interface ISqlAuthorizationRepository
{
    /// <summary>Executes the anonymous-requests procedure and returns the distinct request names.</summary>
    Task<IReadOnlyCollection<string>> GetAnonymousRequestsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Executes the allowed-requests procedure for <paramref name="userId"/> and returns the distinct request
    /// names the user may execute.
    /// </summary>
    Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken);
}
