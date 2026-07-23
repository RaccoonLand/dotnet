using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;

namespace RaccoonLand.Modules.Security.Authorization.SqlServer.Tests.Configuration;

public sealed class SqlAuthorizationOptionsTests
{
    [Fact]
    public void Defaults_MatchDocumentedContract()
    {
        var options = new SqlAuthorizationOptions();

        Assert.Equal("Authorization:SqlServer", SqlAuthorizationOptions.SectionName);
        Assert.Equal(string.Empty, options.ConnectionString);
        Assert.Equal(string.Empty, options.AnonymousRequestsProcedure);
        Assert.Equal(string.Empty, options.AllowedRequestsProcedure);
        Assert.Equal("UserId", options.UserIdParameterName);
        Assert.Equal(30, options.CommandTimeoutSeconds);
        Assert.False(options.EnableCache);
        Assert.Equal("raccoonland:authz:", options.CacheKeyPrefix);
        Assert.Equal(TimeSpan.FromMinutes(5), options.AnonymousCacheDuration);
        Assert.Equal(TimeSpan.FromMinutes(1), options.UserCacheDuration);
    }
}
