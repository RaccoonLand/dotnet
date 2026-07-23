using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Security.Authentication.Configuration;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;

namespace RaccoonLand.Modules.Security.Authentication.Tests.DependencyInjection;

public sealed class DefaultSchemeValidationTests
{
    [Fact]
    public void AddRaccoonLandAuthentication_WhenDefaultNamesRegisteredScheme_Succeeds()
    {
        var services = AuthenticationTestHelpers.CreateServices(new Dictionary<string, string?>
        {
            ["Authentication:DefaultScheme"] = "Bearer",
            ["Authentication:DefaultAuthenticateScheme"] = "Bearer",
            ["Authentication:DefaultChallengeScheme"] = "Bearer",
            ["Authentication:JwtBearer:Bearer:Authority"] = "https://login.example.com",
            ["Authentication:JwtBearer:Bearer:Audience"] = "api",
        });

        var options = AuthenticationTestHelpers.GetOptions(services);
        Assert.Equal("Bearer", options.DefaultScheme);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenNoDefaultSchemesSet_Succeeds()
    {
        var services = AuthenticationTestHelpers.CreateServices(
            AuthenticationTestHelpers.MinimalJwtConfig(defaultScheme: null));

        var options = AuthenticationTestHelpers.GetOptions(services);

        Assert.Null(options.DefaultScheme);
        Assert.Null(options.DefaultAuthenticateScheme);
        Assert.Null(options.DefaultChallengeScheme);
        Assert.Null(options.DefaultSignInScheme);
        Assert.Null(options.DefaultSignOutScheme);
        Assert.True(options.JwtBearer.ContainsKey("Bearer"));
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenUnknownDefaultAndExternalDisabled_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>
                {
                    ["Authentication:AllowExternalDefaultSchemes"] = "false",
                    ["Authentication:DefaultScheme"] = "Cookies",
                    ["Authentication:JwtBearer:Bearer:Authority"] = "https://login.example.com",
                    ["Authentication:JwtBearer:Bearer:Audience"] = "api",
                })));

        Assert.Contains("DefaultScheme", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Cookies", ex.Message, StringComparison.Ordinal);
        Assert.Contains("AllowExternalDefaultSchemes", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenExternalDefaultEnabled_AcceptsExternalName()
    {
        var services = AuthenticationTestHelpers.CreateServices(new Dictionary<string, string?>
        {
            ["Authentication:AllowExternalDefaultSchemes"] = "true",
            ["Authentication:DefaultScheme"] = "Cookies",
            ["Authentication:DefaultAuthenticateScheme"] = "Cookies",
            ["Authentication:DefaultChallengeScheme"] = "oidc",
            ["Authentication:DefaultSignInScheme"] = "Cookies",
            ["Authentication:DefaultSignOutScheme"] = "Cookies",
            ["Authentication:OpenIdConnect:oidc:Authority"] = "https://login.example.com",
            ["Authentication:OpenIdConnect:oidc:ClientId"] = "web",
        });

        var options = AuthenticationTestHelpers.GetOptions(services);
        Assert.Equal("Cookies", options.DefaultScheme);
        Assert.Equal("Cookies", options.DefaultAuthenticateScheme);
        Assert.Equal("oidc", options.DefaultChallengeScheme);
        Assert.Equal("Cookies", options.DefaultSignInScheme);
        Assert.Equal("Cookies", options.DefaultSignOutScheme);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void AddRaccoonLandAuthentication_WhenDefaultSchemeWhitespace_Throws(string whitespace)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>
                {
                    ["Authentication:DefaultScheme"] = whitespace,
                    ["Authentication:JwtBearer:Bearer:Authority"] = "https://login.example.com",
                    ["Authentication:JwtBearer:Bearer:Audience"] = "api",
                })));

        Assert.Contains("cannot be empty or whitespace", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRaccoonLandAuthentication_WhenWhitespaceDefaultAndExternalEnabled_Throws(string whitespace)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>
                {
                    ["Authentication:AllowExternalDefaultSchemes"] = "true",
                    ["Authentication:DefaultSignOutScheme"] = whitespace,
                    ["Authentication:OpenIdConnect:oidc:Authority"] = "https://login.example.com",
                    ["Authentication:OpenIdConnect:oidc:ClientId"] = "web",
                })));

        Assert.Contains(nameof(AuthenticationOptions.DefaultSignOutScheme), ex.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be empty or whitespace", ex.Message, StringComparison.Ordinal);
    }
}
