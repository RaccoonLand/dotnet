using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

        AttachValidation(
            services.AddOptions<SqlAuthorizationOptions>()
                .Bind(configuration.GetSection(sectionName)));

        return services.AddCore();
    }

    /// <summary>Registers the SQL Server provider and configures its options in code.</summary>
    public static IServiceCollection AddRaccoonLandSqlServerAuthorization(
        this IServiceCollection services,
        Action<SqlAuthorizationOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        AttachValidation(
            services.AddOptions<SqlAuthorizationOptions>()
                .Configure(configure));

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton<ISqlAuthorizationRepository, SqlAuthorizationRepository>();
        services.TryAddScoped<IAuthorizationProvider, SqlAuthorizationProvider>();

        return services;
    }

    private static void AttachValidation(OptionsBuilder<SqlAuthorizationOptions> builder)
    {
        builder
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                $"{SqlAuthorizationOptions.SectionName}.{nameof(SqlAuthorizationOptions.ConnectionString)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.AnonymousRequestsProcedure),
                $"{SqlAuthorizationOptions.SectionName}.{nameof(SqlAuthorizationOptions.AnonymousRequestsProcedure)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.AllowedRequestsProcedure),
                $"{SqlAuthorizationOptions.SectionName}.{nameof(SqlAuthorizationOptions.AllowedRequestsProcedure)} is required.")
            .Validate(
                static o => !string.IsNullOrWhiteSpace(o.UserIdParameterName),
                $"{SqlAuthorizationOptions.SectionName}.{nameof(SqlAuthorizationOptions.UserIdParameterName)} is required.")
            .Validate(
                static o => o.CommandTimeoutSeconds > 0,
                $"{SqlAuthorizationOptions.SectionName}.{nameof(SqlAuthorizationOptions.CommandTimeoutSeconds)} must be greater than zero.")
            .ValidateOnStart();
    }
}
