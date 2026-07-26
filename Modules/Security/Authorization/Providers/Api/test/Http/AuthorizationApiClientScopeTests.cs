using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;
using RaccoonLand.Modules.Security.Authorization.Api.Http;

namespace RaccoonLand.Modules.Security.Authorization.Api.Tests.Http;

public sealed class AuthorizationApiClientScopeTests
{
    [Fact]
    public void ApplyScope_WhenUnscoped_ReturnsPathUnchanged()
    {
        var client = CreateClient(new ApiAuthorizationOptions());

        Assert.Equal("anonymous-requests", client.ApplyScope("anonymous-requests"));
    }

    [Fact]
    public void ApplyScope_WhenScopedWithoutPlaceholders_AppendsQueryParameters()
    {
        var client = CreateClient(new ApiAuthorizationOptions
        {
            ServiceName = "Ordering",
            ApplicationName = "Ordering.Api",
        });

        Assert.Equal(
            "anonymous-requests?serviceName=Ordering&applicationName=Ordering.Api",
            client.ApplyScope("anonymous-requests"));
    }

    [Fact]
    public void ApplyScope_WhenPlaceholdersPresent_SubstitutesValues()
    {
        var client = CreateClient(new ApiAuthorizationOptions
        {
            ServiceName = "Ordering",
            ApplicationName = "Ordering.Api",
        });

        Assert.Equal(
            "services/Ordering/apps/Ordering.Api/anonymous-requests",
            client.ApplyScope("services/{serviceName}/apps/{applicationName}/anonymous-requests"));
    }

    [Fact]
    public void ApplyScope_WhenPathAlreadyHasQuery_AppendsWithAmpersand()
    {
        var client = CreateClient(new ApiAuthorizationOptions
        {
            ServiceName = "Ordering",
        });

        Assert.Equal(
            "anonymous-requests?x=1&serviceName=Ordering",
            client.ApplyScope("anonymous-requests?x=1"));
    }

    [Fact]
    public void ApplyScope_EscapesValues()
    {
        var client = CreateClient(new ApiAuthorizationOptions
        {
            ServiceName = "Order Service",
            ApplicationName = "App/A",
        });

        Assert.Equal(
            "anonymous-requests?serviceName=Order%20Service&applicationName=App%2FA",
            client.ApplyScope("anonymous-requests"));
    }

    private static AuthorizationApiClient CreateClient(ApiAuthorizationOptions options)
        => new(new HttpClient(), Options.Create(options));
}
