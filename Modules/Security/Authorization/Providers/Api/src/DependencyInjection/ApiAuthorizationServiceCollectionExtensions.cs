using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Api.Configuration;
using RaccoonLand.Modules.Security.Authorization.Api.Http;
using RaccoonLand.Modules.Security.Authorization.Api.Provider;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration for the API authorization provider. Registers a typed <see cref="AuthorizationApiClient"/> (with
/// the built-in authentication handler) and <see cref="ApiAuthorizationProvider"/> as the active
/// <see cref="IAuthorizationProvider"/>. The provider reads the current user id from
/// <c>ICurrentExecutionContext</c>, which the host must register. Pair this with
/// <c>AddRaccoonLandAuthorization()</c> from the middleware package. When caching is enabled, also register an
/// <c>IDistributedCache</c> (for example <c>services.AddDistributedMemoryCache()</c>).
/// </summary>
public static class ApiAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the API provider and binds its options from the given configuration
    /// <paramref name="sectionName"/> (defaults to <c>Authorization:Api</c>). Use
    /// <paramref name="configureClient"/> to attach custom HTTP handlers (for example an OAuth2
    /// client-credentials or token-propagation handler) to the named client.
    /// </summary>
    public static IServiceCollection AddRaccoonLandApiAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = ApiAuthorizationOptions.SectionName,
        Action<IHttpClientBuilder>? configureClient = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ApiAuthorizationOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(Validate, ValidationMessage);

        return services.AddCore(configureClient);
    }

    /// <summary>Registers the API provider and configures its options in code.</summary>
    public static IServiceCollection AddRaccoonLandApiAuthorization(
        this IServiceCollection services,
        Action<ApiAuthorizationOptions> configure,
        Action<IHttpClientBuilder>? configureClient = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<ApiAuthorizationOptions>()
            .Configure(configure)
            .Validate(Validate, ValidationMessage);

        return services.AddCore(configureClient);
    }

    private static IServiceCollection AddCore(this IServiceCollection services, Action<IHttpClientBuilder>? configureClient)
    {
        services.TryAddTransient<AuthorizationApiAuthenticationHandler>();

        var builder = services
            .AddHttpClient<AuthorizationApiClient>(AuthorizationApiClient.ClientName, ConfigureHttpClient)
            .AddHttpMessageHandler<AuthorizationApiAuthenticationHandler>();

        configureClient?.Invoke(builder);

        services.TryAddScoped<IAuthorizationProvider, ApiAuthorizationProvider>();

        return services;
    }

    private static void ConfigureHttpClient(IServiceProvider provider, HttpClient client)
    {
        var options = provider.GetRequiredService<IOptions<ApiAuthorizationOptions>>().Value;

        if (options.BaseAddress is not null)
        {
            client.BaseAddress = options.BaseAddress;
        }

        client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    }

    private static bool Validate(ApiAuthorizationOptions options)
        => options.BaseAddress is not null
           && !string.IsNullOrWhiteSpace(options.AnonymousRequestsPath)
           && !string.IsNullOrWhiteSpace(options.AllowedRequestsPath)
           && options.AllowedRequestsPath.Contains("{userId}", StringComparison.Ordinal);

    private const string ValidationMessage =
        "ApiAuthorizationOptions requires BaseAddress, AnonymousRequestsPath and AllowedRequestsPath (which must contain the {userId} placeholder).";
}
