using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.Api.Http;

/// <summary>
/// Typed <see cref="HttpClient"/> wrapper around the external authorization API. Each call returns a set of
/// request full-names; the provider checks membership in memory.
/// </summary>
public sealed class AuthorizationApiClient(HttpClient httpClient, IOptions<ApiAuthorizationOptions> options)
{
    /// <summary>Logical name of the registered <see cref="HttpClient"/>.</summary>
    public const string ClientName = "RaccoonLand.Authorization.Api";

    private const string UserIdPlaceholder = "{userId}";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ApiAuthorizationOptions _options = options.Value;

    /// <summary>Calls the anonymous-requests endpoint and returns the distinct request names.</summary>
    public Task<IReadOnlyCollection<string>> GetAnonymousRequestsAsync(CancellationToken cancellationToken)
        => GetRequestSetAsync(_options.AnonymousRequestsPath, cancellationToken);

    /// <summary>Calls the allowed-requests endpoint for <paramref name="userId"/> and returns the distinct request names.</summary>
    public Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken)
    {
        var path = _options.AllowedRequestsPath.Replace(
            UserIdPlaceholder,
            Uri.EscapeDataString(userId),
            StringComparison.Ordinal);

        return GetRequestSetAsync(path, cancellationToken);
    }

    private async Task<IReadOnlyCollection<string>> GetRequestSetAsync(string path, CancellationToken cancellationToken)
    {
        var payload = await _httpClient.GetFromJsonAsync<ApiRequestSet>(path, cancellationToken);

        var set = new HashSet<string>(StringComparer.Ordinal);
        if (payload is null)
        {
            return set;
        }

        foreach (var name in payload.Requests)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                set.Add(name);
            }
        }

        return set;
    }
}
