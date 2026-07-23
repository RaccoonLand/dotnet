using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Security.Authentication.Binding;
using RaccoonLand.Modules.Security.Authentication.Configuration;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Binding;

public sealed class SchemeNameValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MergeJwtBearerSchemes_WhenSchemeNameWhitespace_Throws(string schemeName)
    {
        var scheme = new FakeConfigurationSection(schemeName);
        scheme.AddChild(new FakeConfigurationSection("Authority", "https://login.example.com"));

        var section = new FakeConfigurationSection("JwtBearer");
        section.AddChild(scheme);

        var schemes = new Dictionary<string, JwtBearerOptions>(StringComparer.OrdinalIgnoreCase);

        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationSchemeOptionsBinder.MergeJwtBearerSchemes(section, schemes));

        Assert.Contains("cannot be null, empty, or whitespace", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MergeOpenIdConnectSchemes_WhenSchemeNameWhitespace_Throws(string schemeName)
    {
        var scheme = new FakeConfigurationSection(schemeName);
        scheme.AddChild(new FakeConfigurationSection("Authority", "https://login.example.com"));
        scheme.AddChild(new FakeConfigurationSection("ClientId", "client"));

        var section = new FakeConfigurationSection("OpenIdConnect");
        section.AddChild(scheme);

        var schemes = new Dictionary<string, OpenIdConnectOptions>(StringComparer.OrdinalIgnoreCase);

        var ex = Assert.Throws<InvalidOperationException>(
            () => AuthenticationSchemeOptionsBinder.MergeOpenIdConnectSchemes(section, schemes));

        Assert.Contains("cannot be null, empty, or whitespace", ex.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRaccoonLandAuthentication_WhenConfigureAddsWhitespaceSchemeName_Throws(string schemeName)
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>()),
                configureOptions: options =>
                {
                    options.JwtBearer[schemeName] = new JwtBearerOptions
                    {
                        Authority = "https://login.example.com",
                    };
                }));

        Assert.Contains("cannot be null, empty, or whitespace", ex.Message, StringComparison.Ordinal);
    }
}
