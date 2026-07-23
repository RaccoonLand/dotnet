using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.Security.Authorization.Abstractions;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Configuration;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Data;
using RaccoonLand.Modules.Security.Authorization.SqlServer.Provider;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registration for the SQL Server authorization provider. Registers <see cref="SqlAuthorizationProvider"/> as
/// the active <see cref="IAuthorizationProvider"/>. The provider reads the current user id from
/// <c>ICurrentExecutionContext</c>, which the host must register. Pair this with
/// <c>AddRaccoonLandAuthorization()</c> from the middleware package. When caching is enabled, also register an
/// <c>IDistributedCache</c> (for example <c>services.AddDistributedMemoryCache()</c>).
/// </summary>
public static class SqlAuthorizationServiceCollectionExtensions
{
    /// <summary>
    /// Registers the SQL Server provider and binds its options from the given configuration
    /// <paramref name="sectionName"/> (defaults to <c>Authorization:SqlServer</c>).
    /// </summary>
    public static IServiceCollection AddRaccoonLandSqlServerAuthorization(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SqlAuthorizationOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<SqlAuthorizationOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(Validate, ValidationMessage);

        return services.AddCore();
    }

    /// <summary>Registers the SQL Server provider and configures its options in code.</summary>
    public static IServiceCollection AddRaccoonLandSqlServerAuthorization(
        this IServiceCollection services,
        Action<SqlAuthorizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<SqlAuthorizationOptions>()
            .Configure(configure)
            .Validate(Validate, ValidationMessage);

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ISqlAuthorizationRepository, SqlAuthorizationRepository>();
        services.TryAddScoped<IAuthorizationProvider, SqlAuthorizationProvider>();

        return services;
    }

    private static bool Validate(SqlAuthorizationOptions options)
        => !string.IsNullOrWhiteSpace(options.ConnectionString)
           && !string.IsNullOrWhiteSpace(options.AnonymousRequestsProcedure)
           && !string.IsNullOrWhiteSpace(options.AllowedRequestsProcedure);

    private const string ValidationMessage =
        "SqlAuthorizationOptions requires ConnectionString, AnonymousRequestsProcedure and AllowedRequestsProcedure.";
}
