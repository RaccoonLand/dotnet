using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Security.Authentication.Configuration;
using RaccoonLand.Modules.Security.Authentication.Tests.Support;

namespace RaccoonLand.Modules.Security.Authentication.Tests.DependencyInjection;

public sealed class RegistrationGuardTests
{
    [Fact]
    public void AddRaccoonLandAuthentication_WhenCalledTwice_Throws()
    {
        var services = AuthenticationTestHelpers.CreateServices(
            AuthenticationTestHelpers.MinimalJwtConfig());

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(AuthenticationTestHelpers.MinimalJwtConfig())));

        Assert.Contains("already been called", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenServicesNull_Throws()
    {
        IServiceCollection services = null!;

        Assert.Throws<ArgumentNullException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(AuthenticationTestHelpers.MinimalJwtConfig())));
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenConfigurationNull_Throws()
    {
        var services = new ServiceCollection();

        Assert.Throws<ArgumentNullException>(() =>
            services.AddRaccoonLandAuthentication(configuration: null!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddRaccoonLandAuthentication_WhenSectionNameNullOrWhitespace_Throws(string? sectionName)
    {
        var services = new ServiceCollection();

        Assert.ThrowsAny<ArgumentException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(AuthenticationTestHelpers.MinimalJwtConfig()),
                sectionName: sectionName!));
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenJwtBearerDictionaryNull_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>()),
                configureOptions: options => options.JwtBearer = null!));

        Assert.Contains(nameof(AuthenticationOptions.JwtBearer), ex.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be null", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenOpenIdConnectDictionaryNull_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>()),
                configureOptions: options => options.OpenIdConnect = null!));

        Assert.Contains(nameof(AuthenticationOptions.OpenIdConnect), ex.Message, StringComparison.Ordinal);
        Assert.Contains("cannot be null", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddRaccoonLandAuthentication_WhenSchemeEntryNull_Throws()
    {
        var services = new ServiceCollection();

        var ex = Assert.Throws<InvalidOperationException>(() =>
            services.AddRaccoonLandAuthentication(
                TestConfiguration.FromDictionary(new Dictionary<string, string?>()),
                configureOptions: options =>
                {
                    options.JwtBearer["Bearer"] = null!;
                }));

        Assert.Contains("cannot be null", ex.Message, StringComparison.Ordinal);
        Assert.Contains("Bearer", ex.Message, StringComparison.Ordinal);
    }
}
