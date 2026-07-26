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

        AttachValidation(
            services.AddOptions<ApiAuthorizationOptions>()
                .Bind(configuration.GetSection(sectionName)));

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

        AttachValidation(
            services.AddOptions<ApiAuthorizationOptions>()
                .Configure(configure));

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

    private static void AttachValidation(OptionsBuilder<ApiAuthorizationOptions> builder)
    {
        builder
            .Validate(
                static o => o.BaseAddress is not null,
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.BaseAddress)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.AnonymousRequestsPath),
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.AnonymousRequestsPath)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.AllowedRequestsPath),
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.AllowedRequestsPath)} is required.")
            .Validate(
                static o => string.IsNullOrEmpty(o.AllowedRequestsPath)
                    || o.AllowedRequestsPath.Contains("{userId}", StringComparison.Ordinal),
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.AllowedRequestsPath)} must contain the {{userId}} placeholder.")
            .Validate(
                static o => !PathRequiresPlaceholder(o.AnonymousRequestsPath, o.AllowedRequestsPath, "{serviceName}")
                    || !string.IsNullOrWhiteSpace(o.ServiceName),
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.ServiceName)} is required when a path contains the {{serviceName}} placeholder.")
            .Validate(
                static o => !PathRequiresPlaceholder(o.AnonymousRequestsPath, o.AllowedRequestsPath, "{applicationName}")
                    || !string.IsNullOrWhiteSpace(o.ApplicationName),
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.ApplicationName)} is required when a path contains the {{applicationName}} placeholder.")
            .Validate(
                static o => o.TimeoutSeconds > 0,
                $"{ApiAuthorizationOptions.SectionName}.{nameof(ApiAuthorizationOptions.TimeoutSeconds)} must be greater than zero.")
            .ValidateOnStart();
    }

    private static bool PathRequiresPlaceholder(string anonymousPath, string allowedPath, string placeholder)
        => (!string.IsNullOrEmpty(anonymousPath) && anonymousPath.Contains(placeholder, StringComparison.Ordinal))
           || (!string.IsNullOrEmpty(allowedPath) && allowedPath.Contains(placeholder, StringComparison.Ordinal));
}
