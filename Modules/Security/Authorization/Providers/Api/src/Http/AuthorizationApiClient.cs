using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.Api.Http;

/// <summary>
/// Typed <see cref="HttpClient"/> wrapper around the external authorization API. Each call returns a set of
/// request full-names; the provider checks membership in memory.
/// Non-success responses, timeouts, and JSON failures throw; they are not converted to an authorization deny.
/// </summary>
public sealed class AuthorizationApiClient(HttpClient httpClient, IOptions<ApiAuthorizationOptions> options)
{
    /// <summary>Logical name of the registered <see cref="HttpClient"/>.</summary>
    public const string ClientName = "RaccoonLand.Authorization.Api";

    private const string UserIdPlaceholder = "{userId}";
    private const string ServiceNamePlaceholder = "{serviceName}";
    private const string ApplicationNamePlaceholder = "{applicationName}";

    private readonly HttpClient _httpClient = httpClient;
    private readonly ApiAuthorizationOptions _options = options.Value;

    /// <summary>Calls the anonymous-requests endpoint and returns the distinct request names.</summary>
    public Task<IReadOnlyCollection<string>> GetAnonymousRequestsAsync(CancellationToken cancellationToken)
        => GetRequestSetAsync(ApplyScope(_options.AnonymousRequestsPath), cancellationToken);

    /// <summary>Calls the allowed-requests endpoint for <paramref name="userId"/> and returns the distinct request names.</summary>
    public Task<IReadOnlyCollection<string>> GetAllowedRequestsAsync(string userId, CancellationToken cancellationToken)
    {
        var path = _options.AllowedRequestsPath.Replace(
            UserIdPlaceholder,
            Uri.EscapeDataString(userId),
            StringComparison.Ordinal);

        return GetRequestSetAsync(ApplyScope(path), cancellationToken);
    }

    /// <summary>
    /// Substitutes optional <c>{serviceName}</c> / <c>{applicationName}</c> placeholders. When a scope value
    /// is set but its placeholder is absent from the path, appends it as a query parameter so shared policy
    /// APIs can still isolate callers without requiring a path change.
    /// </summary>
    internal string ApplyScope(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        var result = path;
        var queryParts = new List<string>();

        result = ApplyScopeToken(
            result,
            ServiceNamePlaceholder,
            _options.ServiceName,
            "serviceName",
            queryParts);

        result = ApplyScopeToken(
            result,
            ApplicationNamePlaceholder,
            _options.ApplicationName,
            "applicationName",
            queryParts);

        if (queryParts.Count == 0)
        {
            return result;
        }

        var separator = result.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return result + separator + string.Join("&", queryParts);
    }

    private static string ApplyScopeToken(
        string path,
        string placeholder,
        string value,
        string queryName,
        List<string> queryParts)
    {
        var hasPlaceholder = path.Contains(placeholder, StringComparison.Ordinal);
        var hasValue = !string.IsNullOrWhiteSpace(value);

        if (hasPlaceholder)
        {
            return path.Replace(
                placeholder,
                hasValue ? Uri.EscapeDataString(value.Trim()) : string.Empty,
                StringComparison.Ordinal);
        }

        if (hasValue)
        {
            queryParts.Add(queryName + "=" + Uri.EscapeDataString(value.Trim()));
        }

        return path;
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
