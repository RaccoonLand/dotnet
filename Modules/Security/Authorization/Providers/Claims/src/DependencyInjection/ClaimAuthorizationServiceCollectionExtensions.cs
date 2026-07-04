using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.Claims.Configuration;
using RaccoonLand.Modules.Security.Authorization.Claims.Principals;
using RaccoonLand.Modules.Security.Authorization.Claims.Provider;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration for the claim-based authorization provider. Registers <see cref="ClaimAuthorizationProvider"/>
/// as the active <see cref="IAuthorizationProvider"/> and a default ASP.NET Core
/// <see cref="IClaimsPrincipalAccessor"/> backed by <c>IHttpContextAccessor</c>. Pair this with
/// <c>AddRaccoonLandAuthorization()</c> from the middleware package.
/// </summary>
public static class ClaimAuthorizationServiceCollectionExtensions
{
    /// <summary>Registers the claim provider and configures its options in code.</summary>
    public static IServiceCollection AddRaccoonLandClaimAuthorization(
        this IServiceCollection services,
        Action<ClaimAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        RegisterCore(services);

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }

    /// <summary>
    /// Registers the claim provider and binds its options from <paramref name="configuration"/> (a section
    /// matching <see cref="ClaimAuthorizationConfiguration"/>). The optional <paramref name="configure"/>
    /// delegate runs after binding, so code can add rules — including custom assertions — on top of
    /// configuration.
    /// </summary>
    public static IServiceCollection AddRaccoonLandClaimAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<ClaimAuthorizationOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RegisterCore(services);

        services.Configure<ClaimAuthorizationOptions>(options => ApplyConfiguration(options, configuration));

        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }

    private static void RegisterCore(IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.TryAddSingleton<IClaimsPrincipalAccessor, HttpContextClaimsPrincipalAccessor>();
        services.AddOptions<ClaimAuthorizationOptions>();
        services.TryAddScoped<IAuthorizationProvider, ClaimAuthorizationProvider>();
    }

    private static void ApplyConfiguration(ClaimAuthorizationOptions options, IConfiguration configuration)
    {
        var config = configuration.Get<ClaimAuthorizationConfiguration>();
        if (config is null)
        {
            return;
        }

        foreach (var requestName in config.AnonymousRequests)
        {
            options.AllowAnonymous(requestName);
        }

        foreach (var (requestName, policy) in config.Policies)
        {
            options.RequireAuthenticated(requestName);

            foreach (var claim in policy.Claims)
            {
                options.RequireClaim(requestName, claim.ClaimType, [.. claim.AllowedValues]);
            }
        }
    }
}
