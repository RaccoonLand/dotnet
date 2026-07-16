using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection.Extensions;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.S3;
using RaccoonLand.Modules.FileStorage.S3.Configuration;
using RaccoonLand.Modules.FileStorage.S3.Internal;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Registers the HTTP-based S3 file storage provider and related capabilities.
/// </summary>
public static class S3FileStorageServiceCollectionExtensions
{
    public static IServiceCollection AddRaccoonLandS3FileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName = S3StorageOptions.SectionName)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSharedOptions(configuration);
        services.AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(sectionName))
            .Validate(Validate, "S3 storage options are invalid.");

        return services.AddCore();
    }

    public static IServiceCollection AddRaccoonLandS3FileStorage(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<S3StorageOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSharedOptions(configuration);
        services.AddOptions<S3StorageOptions>()
            .Bind(configuration.GetSection(S3StorageOptions.SectionName))
            .Configure(configure)
            .Validate(Validate, "S3 storage options are invalid.");

        return services.AddCore();
    }

    private static IServiceCollection AddCore(this IServiceCollection services)
    {
        services.AddHttpClient(S3HttpClientName);

        services.TryAddSingleton<S3ObjectClient>(serviceProvider =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<S3StorageOptions>>().Value;
            var settings = S3ConnectionSettings.FromOptions(options);
            var httpClient = serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient(S3HttpClientName);
            httpClient.BaseAddress = settings.ServiceUri;
            httpClient.Timeout = settings.RequestTimeout;
            return new S3ObjectClient(httpClient, settings);
        });

        services.TryAddSingleton<IFileStorage, S3FileStorage>();
        services.TryAddSingleton<IFileUrlSigner, S3FileUrlSigner>();
        services.TryAddSingleton<IMultipartUploadCoordinator, S3MultipartUploadCoordinator>();

        return services;
    }

    private const string S3HttpClientName = "RaccoonLand.FileStorage.S3";

    private static bool Validate(S3StorageOptions options)
        => !string.IsNullOrWhiteSpace(options.Bucket)
           && !string.IsNullOrWhiteSpace(options.Credentials.AccessKeyId)
           && !string.IsNullOrWhiteSpace(options.Credentials.SecretAccessKey);

    private static void AddSharedOptions(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FileStorageOptions>()
            .Bind(configuration.GetSection(FileStorageOptions.SectionName));
    }
}
