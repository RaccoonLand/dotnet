using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;
using JwtBearerHandlerOptions = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerOptions;
using OpenIdConnectHandlerOptions = Microsoft.AspNetCore.Authentication.OpenIdConnect.OpenIdConnectOptions;

namespace RaccoonLand.Modules.Security.Authentication.Tests.ClaimMapping;

public sealed class DisableDefaultClaimMappingTests
{
    [Fact]
    public void AddRaccoonLandAuthentication_DefaultDisableClaimMapping_SetsMapInboundClaimsFalseForJwt()
    {
        var services = AuthenticationTestHelpers.CreateServices(
            AuthenticationTestHelpers.MinimalJwtConfig());

        var monitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<JwtBearerHandlerOptions>>();

        Assert.False(monitor.Get("Bearer").MapInboundClaims);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_DefaultDisableClaimMapping_SetsMapInboundClaimsFalseForOidc()
    {
        var services = AuthenticationTestHelpers.CreateServices(new Dictionary<string, string?>
        {
            ["Authentication:AllowExternalDefaultSchemes"] = "true",
            ["Authentication:DefaultScheme"] = "Cookies",
            ["Authentication:OpenIdConnect:oidc:Authority"] = "https://login.example.com",
            ["Authentication:OpenIdConnect:oidc:ClientId"] = "web",
        });

        var monitor = services.BuildServiceProvider()
            .GetRequiredService<IOptionsMonitor<OpenIdConnectHandlerOptions>>();

        Assert.False(monitor.Get("oidc").MapInboundClaims);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenDisableClaimMappingFalse_LeavesHandlerMapInboundClaims()
    {
        var services = AuthenticationTestHelpers.CreateServices(new Dictionary<string, string?>
        {
            ["Authentication:DefaultScheme"] = "Bearer",
            ["Authentication:DisableDefaultClaimMapping"] = "false",
            ["Authentication:JwtBearer:Bearer:Authority"] = "https://login.example.com",
            ["Authentication:JwtBearer:Bearer:Audience"] = "api",
            ["Authentication:AllowExternalDefaultSchemes"] = "true",
            ["Authentication:OpenIdConnect:oidc:Authority"] = "https://login.example.com",
            ["Authentication:OpenIdConnect:oidc:ClientId"] = "web",
        });

        var provider = services.BuildServiceProvider();
        var jwt = provider.GetRequiredService<IOptionsMonitor<JwtBearerHandlerOptions>>().Get("Bearer");
        var oidc = provider.GetRequiredService<IOptionsMonitor<OpenIdConnectHandlerOptions>>().Get("oidc");

        Assert.True(jwt.MapInboundClaims);
        Assert.True(oidc.MapInboundClaims);
    }
}
