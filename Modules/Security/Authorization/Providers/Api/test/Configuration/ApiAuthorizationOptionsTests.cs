using RaccoonLand.Modules.Security.Authorization.Api.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.Api.Tests.Configuration;

public sealed class ApiAuthorizationOptionsTests
{
    [Fact]
    public void Defaults_MatchDocumentedContract()
    {
        var options = new ApiAuthorizationOptions();

        Assert.Equal("Authorization:Api", ApiAuthorizationOptions.SectionName);
        Assert.Null(options.BaseAddress);
        Assert.Equal("anonymous-requests", options.AnonymousRequestsPath);
        Assert.Equal("users/{userId}/allowed-requests", options.AllowedRequestsPath);
        Assert.Equal(30, options.TimeoutSeconds);
        Assert.Equal(AuthorizationApiAuthenticationMode.None, options.AuthenticationMode);
        Assert.Equal("X-Api-Key", options.ApiKeyHeaderName);
        Assert.Null(options.ApiKey);
        Assert.Null(options.BearerToken);
        Assert.False(options.EnableCache);
        Assert.Equal("raccoonland:authz:", options.CacheKeyPrefix);
        Assert.Equal(TimeSpan.FromMinutes(5), options.AnonymousCacheDuration);
        Assert.Equal(TimeSpan.FromMinutes(1), options.UserCacheDuration);
    }
}
