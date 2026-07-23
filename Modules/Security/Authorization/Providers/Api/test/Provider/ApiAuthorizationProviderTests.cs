using System.Net;
using System.Text.Json;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;
using RaccoonLand.Modules.Security.Authorization.Api.Tests.Support;

namespace RaccoonLand.Modules.Security.Authorization.Api.Tests.Provider;

public sealed class ApiAuthorizationProviderTests
{
    private const string RequestName = "Sample.Namespace.SampleRequest";

    [Fact]
    public async Task AuthorizeAsync_WhenAnonymous_AllowsEvenWithoutUser()
    {
        using var harness = ApiAuthorizationTestHelpers.CreateProvider(
            responder: _ => ApiAuthorizationTestHelpers.JsonOk(
                ApiAuthorizationTestHelpers.RequestsJson(RequestName)),
            userId: null);

        var decision = await Authorize(harness.Provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenNoUserId_ReturnsUnauthenticated()
    {
        using var harness = ApiAuthorizationTestHelpers.CreateProvider(
            responder: _ => ApiAuthorizationTestHelpers.JsonOk(
                ApiAuthorizationTestHelpers.RequestsJson()),
            userId: null);

        var decision = await Authorize(harness.Provider);

        Assert.Equal(AuthorizationStatus.Unauthenticated, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequestInUserAllowedSet_ReturnsAllowed()
    {
        using var harness = ApiAuthorizationTestHelpers.CreateProvider(
            responder: request => request.RequestUri!.AbsolutePath.Contains("allowed-requests", StringComparison.Ordinal)
                ? ApiAuthorizationTestHelpers.JsonOk(ApiAuthorizationTestHelpers.RequestsJson(RequestName))
                : ApiAuthorizationTestHelpers.JsonOk(ApiAuthorizationTestHelpers.RequestsJson()),
            userId: "user-1");

        var decision = await Authorize(harness.Provider);

        Assert.Equal(AuthorizationStatus.Allowed, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenRequestNotInUserAllowedSet_ReturnsDenied()
    {
        using var harness = ApiAuthorizationTestHelpers.CreateProvider(
            responder: request => request.RequestUri!.AbsolutePath.Contains("allowed-requests", StringComparison.Ordinal)
                ? ApiAuthorizationTestHelpers.JsonOk(ApiAuthorizationTestHelpers.RequestsJson("Other.Request"))
                : ApiAuthorizationTestHelpers.JsonOk(ApiAuthorizationTestHelpers.RequestsJson()),
            userId: "user-1");

        var decision = await Authorize(harness.Provider);

        Assert.Equal(AuthorizationStatus.Denied, decision.Status);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenTransportFails_PropagatesExceptionNotDenied()
    {
        using var harness = ApiAuthorizationTestHelpers.CreateProvider(
            responder: _ => new HttpResponseMessage(HttpStatusCode.InternalServerError),
            userId: "user-1");

        await Assert.ThrowsAsync<HttpRequestException>(() => Authorize(harness.Provider));
    }

    [Fact]
    public async Task AuthorizeAsync_WhenMalformedResponse_PropagatesExceptionNotDenied()
    {
        using var harness = ApiAuthorizationTestHelpers.CreateProvider(
            responder: _ => ApiAuthorizationTestHelpers.JsonOk("{ this is not valid json"),
            userId: "user-1");

        await Assert.ThrowsAsync<JsonException>(() => Authorize(harness.Provider));
    }

    [Fact]
    public void Constructor_WhenCacheEnabledWithoutDistributedCache_Throws()
    {
        var options = new ApiAuthorizationOptions
        {
            BaseAddress = new Uri(ApiAuthorizationTestHelpers.BaseAddress),
            EnableCache = true,
        };

        Assert.Throws<InvalidOperationException>(() =>
            ApiAuthorizationTestHelpers.CreateProvider(
                responder: _ => ApiAuthorizationTestHelpers.JsonOk(ApiAuthorizationTestHelpers.RequestsJson()),
                userId: "user-1",
                options: options));
    }

    private static Task<AuthorizationDecision> Authorize(Api.Provider.ApiAuthorizationProvider provider)
        => provider.AuthorizeAsync(new AuthorizationContext(RequestName), CancellationToken.None);
}
