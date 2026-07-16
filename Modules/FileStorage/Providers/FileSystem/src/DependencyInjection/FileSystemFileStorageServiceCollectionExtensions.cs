using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.FileSystem;
using RaccoonLand.Modules.FileStorage.FileSystem.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the local file system file storage provider as the active <see cref="IFileStorage"/> implementation.
/// </summary>
public static class FileSystemFileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddRaccoonLandFileSystemStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = FileSystemStorageOptions.SectionName,
        string sharedSectionName = FileStorageOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSharedOptions(configuration, sharedSectionName);
        services.AddOptions<FileSystemStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath), "RootPath is required.");

        return services.AddCore();
    }

    public static IServiceCollection AddRaccoonLandFileSystemStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<FileSystemStorageOptions> configure,
        string sectionName = FileSystemStorageOptions.SectionName,
        string sharedSectionName = FileStorageOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSharedOptions(configuration, sharedSectionName);
        services.AddOptions<FileSystemStorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Configure(configure)
            .Validate(options => !string.IsNullOrWhiteSpace(options.RootPath), "RootPath is required.");

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.TryAddSingleton<IFileStorage, FileSystemFileStorage>();
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
