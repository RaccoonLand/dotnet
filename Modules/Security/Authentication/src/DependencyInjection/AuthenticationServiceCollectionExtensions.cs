using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RaccoonLand.Modules.Security.Authentication.Binding;
using RaccoonLand.Modules.Security.Authentication.ClaimMapping;
using RaccoonLand.Modules.Security.Authentication.Cloning;
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
    /// Calling this method more than once is not supported.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="sectionName">
    /// Root configuration section name (defaults to <see cref="RaccoonLandAuthenticationOptions.SectionName"/>).
    /// </param>
    /// <param name="configureOptions">
    /// Optional post-bind customization of <see cref="RaccoonLandAuthenticationOptions"/>.
    /// Applied after configuration binding and before validation and scheme registration.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when this method was already called, scheme dictionaries or entries are null, scheme names collide
    /// or are invalid, or a default scheme is not among the schemes registered by this call (unless
    /// <see cref="RaccoonLandAuthenticationOptions.AllowExternalDefaultSchemes"/> is <see langword="true"/>).
    /// </exception>
    public static IServiceCollection AddRaccoonLandAuthentication(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = RaccoonLandAuthenticationOptions.SectionName,
        Action<RaccoonLandAuthenticationOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

        if (services.Any(static descriptor => descriptor.ServiceType == typeof(RaccoonLandAuthenticationMarker)))
        {
            throw new InvalidOperationException(
                $"{nameof(AddRaccoonLandAuthentication)} has already been called. Calling it more than once is not supported.");
        }

        var options = BuildOptions(configuration, sectionName, configureOptions);
        ValidateOptions(options);

        // Mutate DI only after validation. Marker and IOptions are registered last so a failure
        // during scheme registration does not mark the call as completed. Absence of the marker
        // after a failure does not make retrying on the same IServiceCollection safe.
        var authenticationBuilder = services.AddAuthentication(authenticationOptions =>
        {
            ApplyDefaultSchemes(authenticationOptions, options);
        });

        RegisterJwtBearerSchemes(authenticationBuilder, options);
        RegisterOpenIdConnectSchemes(authenticationBuilder, options);

        if (options.ClearGlobalJwtClaimTypeMaps)
        {
            ClaimMappingConfiguration.ClearGlobalJwtClaimTypeMaps();
        }

        services.AddSingleton(Options.Create(options));
        services.AddSingleton<RaccoonLandAuthenticationMarker>();

        return services;
    }

    private static RaccoonLandAuthenticationOptions BuildOptions(
        IConfiguration configuration,
        string sectionName,
        Action<RaccoonLandAuthenticationOptions>? configureOptions)
    {
        var section = configuration.GetSection(sectionName);
        var options = new RaccoonLandAuthenticationOptions();

        // Bind root scalars once. Scheme dictionaries are bound only via Merge* below
        // so collection properties (for example Scope) are not bound twice.
        BindRootProperties(section, options);

        AuthenticationSchemeOptionsBinder.MergeJwtBearerSchemes(
            section.GetSection(nameof(RaccoonLandAuthenticationOptions.JwtBearer)),
            options.JwtBearer);
        AuthenticationSchemeOptionsBinder.MergeOpenIdConnectSchemes(
            section.GetSection(nameof(RaccoonLandAuthenticationOptions.OpenIdConnect)),
            options.OpenIdConnect);

        configureOptions?.Invoke(options);
        return options;
    }

    private static void BindRootProperties(
        IConfigurationSection section,
        RaccoonLandAuthenticationOptions options)
    {
        var root = section.Get<AuthenticationRootSettings>() ?? new AuthenticationRootSettings();

        options.DefaultScheme = root.DefaultScheme;
        options.DefaultAuthenticateScheme = root.DefaultAuthenticateScheme;
        options.DefaultChallengeScheme = root.DefaultChallengeScheme;
        options.DefaultSignInScheme = root.DefaultSignInScheme;
        options.DefaultSignOutScheme = root.DefaultSignOutScheme;
        options.DisableDefaultClaimMapping = root.DisableDefaultClaimMapping;
        options.ClearGlobalJwtClaimTypeMaps = root.ClearGlobalJwtClaimTypeMaps;
        options.AllowExternalDefaultSchemes = root.AllowExternalDefaultSchemes;
    }

    private static void ValidateOptions(RaccoonLandAuthenticationOptions options)
    {
        if (options.JwtBearer is null)
        {
            throw new InvalidOperationException(
                $"{nameof(RaccoonLandAuthenticationOptions.JwtBearer)} cannot be null. " +
                $"Use an empty dictionary when no JWT Bearer schemes are required.");
        }

        if (options.OpenIdConnect is null)
        {
            throw new InvalidOperationException(
                $"{nameof(RaccoonLandAuthenticationOptions.OpenIdConnect)} cannot be null. " +
                $"Use an empty dictionary when no OpenID Connect schemes are required.");
        }

        EnsureSchemeDictionaryEntries(options.JwtBearer, nameof(RaccoonLandAuthenticationOptions.JwtBearer));
        EnsureSchemeDictionaryEntries(options.OpenIdConnect, nameof(RaccoonLandAuthenticationOptions.OpenIdConnect));

        var collisions = options.JwtBearer.Keys
            .Intersect(options.OpenIdConnect.Keys, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (collisions.Length > 0)
        {
            throw new InvalidOperationException(
                "Authentication scheme name collision detected between JwtBearer and OpenIdConnect " +
                "(case-insensitive): " + string.Join(", ", collisions));
        }

        var registeredSchemes = new HashSet<string>(
            options.JwtBearer.Keys.Concat(options.OpenIdConnect.Keys),
            StringComparer.OrdinalIgnoreCase);

        EnsureDefaultScheme(
            options.DefaultScheme,
            registeredSchemes,
            nameof(options.DefaultScheme),
            options.AllowExternalDefaultSchemes);
        EnsureDefaultScheme(
            options.DefaultAuthenticateScheme,
            registeredSchemes,
            nameof(options.DefaultAuthenticateScheme),
            options.AllowExternalDefaultSchemes);
        EnsureDefaultScheme(
            options.DefaultChallengeScheme,
            registeredSchemes,
            nameof(options.DefaultChallengeScheme),
            options.AllowExternalDefaultSchemes);
        EnsureDefaultScheme(
            options.DefaultSignInScheme,
            registeredSchemes,
            nameof(options.DefaultSignInScheme),
            options.AllowExternalDefaultSchemes);
        EnsureDefaultScheme(
            options.DefaultSignOutScheme,
            registeredSchemes,
            nameof(options.DefaultSignOutScheme),
            options.AllowExternalDefaultSchemes);
    }

    private static void EnsureSchemeDictionaryEntries<TOptions>(
        Dictionary<string, TOptions> schemes,
        string dictionaryName)
        where TOptions : class
    {
        var seenKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (schemeName, schemeOptions) in schemes)
        {
            EnsureSchemeName(schemeName, dictionaryName);

            if (!seenKeys.Add(schemeName))
            {
                throw new InvalidOperationException(
                    $"Duplicate authentication scheme name under '{dictionaryName}' (case-insensitive): '{schemeName}'.");
            }

            if (schemeOptions is null)
            {
                throw new InvalidOperationException(
                    $"Authentication scheme '{schemeName}' under '{dictionaryName}' cannot be null.");
            }
        }
    }

    private static void EnsureSchemeName(string schemeName, string dictionaryName)
    {
        if (string.IsNullOrWhiteSpace(schemeName))
        {
            throw new InvalidOperationException(
                $"Authentication scheme name under '{dictionaryName}' cannot be null, empty, or whitespace.");
        }
    }

    private static void EnsureDefaultScheme(
        string? schemeName,
        IReadOnlySet<string> registeredSchemes,
        string propertyName,
        bool allowExternalDefaultSchemes)
    {
        if (schemeName is null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(schemeName))
        {
            throw new InvalidOperationException(
                $"{propertyName} cannot be empty or whitespace.");
        }

        if (allowExternalDefaultSchemes || registeredSchemes.Contains(schemeName))
        {
            return;
        }

        var available = registeredSchemes.Count == 0
            ? "(none)"
            : string.Join(", ", registeredSchemes.OrderBy(static name => name, StringComparer.OrdinalIgnoreCase));

        throw new InvalidOperationException(
            $"{propertyName} '{schemeName}' is not among the schemes registered by this call: {available}. " +
            $"Set {nameof(RaccoonLandAuthenticationOptions.AllowExternalDefaultSchemes)} to true to allow " +
            "defaults that name schemes registered outside this package (for example Cookie + OpenID Connect).");
    }

    private static void ApplyDefaultSchemes(
        Microsoft.AspNetCore.Authentication.AuthenticationOptions authenticationOptions,
        RaccoonLandAuthenticationOptions options)
    {
        if (options.DefaultScheme is not null)
        {
            authenticationOptions.DefaultScheme = options.DefaultScheme;
        }

        if (options.DefaultAuthenticateScheme is not null)
        {
            authenticationOptions.DefaultAuthenticateScheme = options.DefaultAuthenticateScheme;
        }

        if (options.DefaultChallengeScheme is not null)
        {
            authenticationOptions.DefaultChallengeScheme = options.DefaultChallengeScheme;
        }

        if (options.DefaultSignInScheme is not null)
        {
            authenticationOptions.DefaultSignInScheme = options.DefaultSignInScheme;
        }

        if (options.DefaultSignOutScheme is not null)
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

    /// <summary>
    /// Root scalar settings only. Scheme dictionaries are excluded so they are bound once via Merge*.
    /// </summary>
    private sealed class AuthenticationRootSettings
    {
        public string? DefaultScheme { get; set; }
        public string? DefaultAuthenticateScheme { get; set; }
        public string? DefaultChallengeScheme { get; set; }
        public string? DefaultSignInScheme { get; set; }
        public string? DefaultSignOutScheme { get; set; }
        public bool DisableDefaultClaimMapping { get; set; } = true;
        public bool ClearGlobalJwtClaimTypeMaps { get; set; }
        public bool AllowExternalDefaultSchemes { get; set; }
    }
}
