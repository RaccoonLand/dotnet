using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Modules.Security.Authentication.Configuration;
using RaccoonLandAuthenticationOptions = RaccoonLand.Modules.Security.Authentication.Configuration.AuthenticationOptions;

namespace RaccoonLand.Modules.Security.Authentication;

/// <summary>
/// Registers configuration-driven JWT Bearer and OpenID Connect authentication schemes.
/// Does not call <c>UseAuthentication</c>, <c>UseAuthorization</c>, or register authorization policies;
/// those remain the application's responsibility.
/// </summary>
public static class AuthenticationServiceCollectionExtensions
{
    /// <summary>
    /// Registers authentication from a configuration section
    /// (<c>appsettings.json</c>, environment-specific files, environment variables, user secrets, Key Vault, and so on).
    /// Nested scheme keys such as <c>JwtBearer</c> and <c>OpenIdConnect</c> are fixed relative to the root section.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="sectionName">
    /// Root configuration section name (defaults to <see cref="RaccoonLandAuthenticationOptions.SectionName"/>).
    /// </param>
    /// <param name="configureOptions">Optional post-bind customization of <see cref="RaccoonLandAuthenticationOptions"/>.</param>
    public static IServiceCollection AddRaccoonLandAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RaccoonLandAuthenticationOptions.SectionName,
        Action<RaccoonLandAuthenticationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(sectionName);
        services.Configure<RaccoonLandAuthenticationOptions>(section);

        if (configureOptions is not null)
        {
            services.Configure(configureOptions);
        }

        var options = section.Get<RaccoonLandAuthenticationOptions>() ?? new RaccoonLandAuthenticationOptions();

        AuthenticationSchemeOptionsBinder.MergeJwtBearerSchemes(
            section.GetSection(nameof(RaccoonLandAuthenticationOptions.JwtBearer)),
            options.JwtBearer);
        AuthenticationSchemeOptionsBinder.MergeOpenIdConnectSchemes(
            section.GetSection(nameof(RaccoonLandAuthenticationOptions.OpenIdConnect)),
            options.OpenIdConnect);

        configureOptions?.Invoke(options);

        if (options.DisableDefaultClaimMapping)
        {
            ClaimMappingConfiguration.DisableDefaultClaimMapping();
        }

        var authenticationBuilder = services.AddAuthentication(authenticationOptions =>
        {
            ApplyDefaultSchemes(authenticationOptions, options);
        });

        RegisterJwtBearerSchemes(authenticationBuilder, options);
        RegisterOpenIdConnectSchemes(authenticationBuilder, options);

        return services;
    }

    private static void ApplyDefaultSchemes(
        Microsoft.AspNetCore.Authentication.AuthenticationOptions authenticationOptions,
        RaccoonLandAuthenticationOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.DefaultScheme))
        {
            authenticationOptions.DefaultScheme = options.DefaultScheme;
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultAuthenticateScheme))
        {
            authenticationOptions.DefaultAuthenticateScheme = options.DefaultAuthenticateScheme;
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultChallengeScheme))
        {
            authenticationOptions.DefaultChallengeScheme = options.DefaultChallengeScheme;
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultSignInScheme))
        {
            authenticationOptions.DefaultSignInScheme = options.DefaultSignInScheme;
        }

        if (!string.IsNullOrWhiteSpace(options.DefaultSignOutScheme))
        {
            authenticationOptions.DefaultSignOutScheme = options.DefaultSignOutScheme;
        }
    }

    private static void RegisterJwtBearerSchemes(
        AuthenticationBuilder authenticationBuilder,
        RaccoonLandAuthenticationOptions options)
    {
        foreach (var (schemeName, schemeOptions) in options.JwtBearer)
        {
            authenticationBuilder.AddJwtBearer(schemeName, jwtBearerOptions =>
            {
                AuthenticationSchemeOptionsCloner.Populate(schemeOptions, jwtBearerOptions);

                if (options.DisableDefaultClaimMapping)
                {
                    jwtBearerOptions.MapInboundClaims = false;
                }
            });
        }
    }

    private static void RegisterOpenIdConnectSchemes(
        AuthenticationBuilder authenticationBuilder,
        RaccoonLandAuthenticationOptions options)
    {
        foreach (var (schemeName, schemeOptions) in options.OpenIdConnect)
        {
            authenticationBuilder.AddOpenIdConnect(schemeName, openIdConnectOptions =>
            {
                AuthenticationSchemeOptionsCloner.Populate(schemeOptions, openIdConnectOptions);

                if (options.DisableDefaultClaimMapping)
                {
                    openIdConnectOptions.MapInboundClaims = false;
                }
            });
        }
    }
}
