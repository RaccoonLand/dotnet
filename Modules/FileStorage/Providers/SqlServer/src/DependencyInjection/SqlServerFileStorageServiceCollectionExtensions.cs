using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.SqlServer;
using RaccoonLand.Modules.FileStorage.SqlServer.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the SQL Server file storage provider as the active <see cref="IFileStorage"/> implementation.
/// </summary>
public static class SqlServerFileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddRaccoonLandSqlServerFileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = SqlServerStorageOptions.SectionName,
        string sharedSectionName = FileStorageOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSharedOptions(configuration, sharedSectionName);
        services.AddOptions<SqlServerStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "ConnectionString is required.");

        return services.AddCore();
    }

    public static IServiceCollection AddRaccoonLandSqlServerFileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<SqlServerStorageOptions> configure,
        string sectionName = SqlServerStorageOptions.SectionName,
        string sharedSectionName = FileStorageOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSharedOptions(configuration, sharedSectionName);
        services.AddOptions<SqlServerStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Configure(configure)
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ConnectionString),
                "ConnectionString is required.");

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IFileStorage, SqlServerFileStorage>();
        return services;
    }

    private static void AddSharedOptions(
        this IServiceCollection services,
        IConfiguration configuration,
        string sharedSectionName)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(sharedSectionName));
    }
}
