using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authentication.Configuration;

namespace RaccoonLand.Modules.Security.Authentication.Tests.Support;

internal static class AuthenticationTestHelpers
{
    public static IServiceCollection CreateServices(
        IDictionary<string, string?> configuration,
        Action<AuthenticationOptions>? configureOptions = null)
    {
        var services = new ServiceCollection();
        services.AddRaccoonLandAuthentication(
            TestConfiguration.FromDictionary(configuration),
            configureOptions: configureOptions);
        return services;
    }

    public static AuthenticationOptions GetOptions(IServiceCollection services)
        => services.BuildServiceProvider()
            .GetRequiredService<IOptions<AuthenticationOptions>>().Value;

    public static Dictionary<string, string?> MinimalJwtConfig(
        string schemeName = "Bearer",
        string? defaultScheme = "Bearer")
    {
        var values = new Dictionary<string, string?>
        {
            [$"Authentication:JwtBearer:{schemeName}:Authority"] = "https://login.example.com",
            [$"Authentication:JwtBearer:{schemeName}:Audience"] = "api",
        };

        if (defaultScheme is not null)
        {
            values["Authentication:DefaultScheme"] = defaultScheme;
        }

        return values;
    }
}
