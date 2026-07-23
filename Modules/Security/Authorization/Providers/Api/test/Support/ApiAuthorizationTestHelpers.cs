using System.Net;
using System.Text;
using Microsoft.Extensions.Options;
using RaccoonLand.Core.ExecutionContext.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;
using RaccoonLand.Modules.Security.Authorization.Api.Http;
using RaccoonLand.Modules.Security.Authorization.Api.Provider;

namespace RaccoonLand.Modules.Security.Authorization.Api.Tests.Support;

internal sealed class FakeExecutionContext : ICurrentExecutionContext
{
    public bool IsAvailable => UserId is not null;
    public string? UserId { get; init; }
    public string? TenantId { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>Routes each request to a supplied responder and records the paths that were called.</summary>
internal sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    : HttpMessageHandler
{
    public List<string> RequestedPaths { get; } = [];

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        RequestedPaths.Add(request.RequestUri!.AbsolutePath);
        return Task.FromResult(responder(request));
    }
}

/// <summary>Owns the <see cref="HttpClient"/> backing a provider so tests can dispose it deterministically.</summary>
internal sealed class ApiProviderHarness(ApiAuthorizationProvider provider, HttpClient httpClient) : IDisposable
{
    public ApiAuthorizationProvider Provider { get; } = provider;

    public void Dispose() => httpClient.Dispose();
}

internal static class ApiAuthorizationTestHelpers
{
    public const string BaseAddress = "https://policy.internal/api/";

    public static HttpResponseMessage JsonOk(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    public static string RequestsJson(params string[] names)
        => "{\"requests\":[" + string.Join(",", names.Select(n => "\"" + n + "\"")) + "]}";

    public static ApiProviderHarness CreateProvider(
        Func<HttpRequestMessage, HttpResponseMessage> responder,
        string? userId,
        ApiAuthorizationOptions? options = null)
    {
        options ??= new ApiAuthorizationOptions { BaseAddress = new Uri(BaseAddress) };

        var handler = new StubHttpMessageHandler(responder);
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseAddress) };

        try
        {
            var client = new AuthorizationApiClient(httpClient, Options.Create(options));
            var provider = new ApiAuthorizationProvider(
                client,
                new FakeExecutionContext { UserId = userId },
                Options.Create(options));

            return new ApiProviderHarness(provider, httpClient);
        }
        catch
        {
            httpClient.Dispose();
            throw;
        }
    }
}
