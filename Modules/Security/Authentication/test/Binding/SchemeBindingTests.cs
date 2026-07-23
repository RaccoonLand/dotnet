using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authentication.Configuration;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Binding;

public sealed class SchemeBindingTests
{
    [Fact]
    public void AddRaccoonLandAuthentication_BindsOidcScopesOnceWithoutDuplication()
    {
        var services = AuthenticationTestHelpers.CreateServices(new Dictionary<string, string?>
        {
            ["Authentication:AllowExternalDefaultSchemes"] = "true",
            ["Authentication:DefaultScheme"] = "Cookies",
            ["Authentication:OpenIdConnect:oidc:Authority"] = "https://login.example.com",
            ["Authentication:OpenIdConnect:oidc:ClientId"] = "web",
            ["Authentication:OpenIdConnect:oidc:Scope:0"] = "openid",
            ["Authentication:OpenIdConnect:oidc:Scope:1"] = "profile",
            ["Authentication:OpenIdConnect:oidc:Scope:2"] = "email",
        });

        var options = AuthenticationTestHelpers.GetOptions(services);
        var scopes = options.OpenIdConnect["oidc"].Scope.ToArray();

        Assert.Equal(["openid", "profile", "email"], scopes);
        Assert.Equal(3, scopes.Length);
        Assert.Equal(1, scopes.Count(s => s == "openid"));
        Assert.Equal(1, scopes.Count(s => s == "profile"));
        Assert.Equal(1, scopes.Count(s => s == "email"));
    }

    [Fact]
    public void AddRaccoonLandAuthentication_ConfigureOptionsOverridesConfiguration()
    {
        var services = AuthenticationTestHelpers.CreateServices(
            new Dictionary<string, string?>
            {
                ["Authentication:DefaultScheme"] = "Bearer",
                ["Authentication:JwtBearer:Bearer:Authority"] = "https://from-config.example.com",
                ["Authentication:JwtBearer:Bearer:Audience"] = "config-audience",
            },
            configureOptions: options =>
            {
                options.JwtBearer["Bearer"].Authority = "https://from-code.example.com";
                options.JwtBearer["Bearer"].Audience = "code-audience";
            });

        var options = AuthenticationTestHelpers.GetOptions(services);

        Assert.Equal("https://from-code.example.com", options.JwtBearer["Bearer"].Authority);
        Assert.Equal("code-audience", options.JwtBearer["Bearer"].Audience);
    }
}
