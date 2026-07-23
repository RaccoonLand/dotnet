using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Security.Authentication.Binding;
using RaccoonLand.Modules.Security.Authentication.Configuration;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Binding;

public sealed class SchemeCollisionTests
{
    [Fact]
    public void MergeJwtBearerSchemes_WhenNamesDifferOnlyByCase_Throws()
    {
        var bearer = new FakeConfigurationSection("Bearer");
        bearer.AddChild(new FakeConfigurationSection("Authority", "https://a.example.com"));

        var bearerLower = new FakeConfigurationSection("bearer");
        bearerLower.AddChild(new FakeConfigurationSection("Authority", "https://b.example.com"));

        var schemesSection = new FakeConfigurationSection("JwtBearer");
        schemesSection.AddChild(bearer);
        schemesSection.AddChild(bearerLower);

        var schemes = new Dictionary<string, JwtBearerOptions>(StringComparer.OrdinalIgnoreCase);

        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationSchemeOptionsBinder.MergeJwtBearerSchemes(schemesSection, schemes));

        Assert.Contains("Duplicate authentication scheme name", ex.Message, StringComparison.Ordinal);
        Assert.Contains("case-insensitive", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenJwtAndOidcNamesDifferOnlyByCase_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>
                {
                    ["Authentication:JwtBearer:Bearer:Authority"] = "https://a.example.com",
                    ["Authentication:JwtBearer:Bearer:Audience"] = "api",
                    ["Authentication:OpenIdConnect:bearer:Authority"] = "https://b.example.com",
                    ["Authentication:OpenIdConnect:bearer:ClientId"] = "client",
                })));

        Assert.Contains("scheme name collision", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("case-insensitive", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenSchemeNamesAreDistinct_Succeeds()
    {
        var services = AuthenticationTestHelpers.CreateServices(new Dictionary<string, string?>
        {
            ["Authentication:DefaultScheme"] = "Bearer",
            ["Authentication:JwtBearer:Bearer:Authority"] = "https://a.example.com",
            ["Authentication:JwtBearer:Bearer:Audience"] = "api",
            ["Authentication:JwtBearer:Partner:Authority"] = "https://b.example.com",
            ["Authentication:JwtBearer:Partner:Audience"] = "partner",
            ["Authentication:OpenIdConnect:oidc:Authority"] = "https://c.example.com",
            ["Authentication:OpenIdConnect:oidc:ClientId"] = "web",
        });

        var options = AuthenticationTestHelpers.GetOptions(services);

        Assert.Equal(2, options.JwtBearer.Count);
        Assert.Single(options.OpenIdConnect);
        Assert.True(options.JwtBearer.ContainsKey("Bearer"));
        Assert.True(options.JwtBearer.ContainsKey("Partner"));
        Assert.True(options.OpenIdConnect.ContainsKey("oidc"));
    }
}
