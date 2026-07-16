using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.FileStorage.S3.Configuration;

namespace RaccoonLand.Modules.FileStorage.S3.Internal;

internal sealed class S3ConnectionSettings
{
    public required string Bucket { get; init; }

    public required string Region { get; init; }

    public required Uri ServiceUri { get; init; }

    public required bool ForcePathStyle { get; init; }

    public required string AccessKeyId { get; init; }

    public required string SecretAccessKey { get; init; }

    public string? SessionToken { get; init; }

    public string? KeyPrefix { get; init; }

    public TimeSpan RequestTimeout { get; init; } = TimeSpan.FromMinutes(5);

    public static S3ConnectionSettings FromOptions(S3StorageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Bucket))
        {
            throw new FileStorageConfigurationException("S3 bucket is required.");
        }

        if (string.IsNullOrWhiteSpace(options.Credentials.AccessKeyId)
            || string.IsNullOrWhiteSpace(options.Credentials.SecretAccessKey))
        {
            throw new FileStorageConfigurationException("S3 credentials require AccessKeyId and SecretAccessKey.");
        }

        var region = string.IsNullOrWhiteSpace(options.Region) ? "us-east-1" : options.Region.Trim();
        var forcePathStyle = ResolveForcePathStyle(options);
        var serviceUri = ResolveServiceUri(options, region, forcePathStyle);

        return new S3ConnectionSettings
        {
            Bucket = options.Bucket.Trim(),
            Region = region,
            ServiceUri = serviceUri,
            ForcePathStyle = forcePathStyle,
            AccessKeyId = options.Credentials.AccessKeyId.Trim(),
            SecretAccessKey = options.Credentials.SecretAccessKey,
            SessionToken = options.Credentials.SessionToken,
            KeyPrefix = string.IsNullOrWhiteSpace(options.KeyPrefix) ? null : options.KeyPrefix.Trim().Trim('/'),
            RequestTimeout = options.Compatibility?.RequestTimeout ?? TimeSpan.FromMinutes(5),
        };
    }

    public string ToObjectKey(string key)
        => KeyPrefix is null ? key : $"{KeyPrefix}/{key}";

    private static bool ResolveForcePathStyle(S3StorageOptions options)
    {
        if (options.Compatibility?.ForcePathStyle is bool forcePathStyle)
        {
            return forcePathStyle;
        }

        return options.Flavor is not S3Flavor.Aws || !string.IsNullOrWhiteSpace(options.Endpoint);
    }

    private static Uri ResolveServiceUri(S3StorageOptions options, string region, bool forcePathStyle)
    {
        if (!string.IsNullOrWhiteSpace(options.Endpoint))
        {
            return new Uri(options.Endpoint.Trim(), UriKind.Absolute);
        }

        if (forcePathStyle)
        {
            return new Uri($"https://s3.{region}.amazonaws.com", UriKind.Absolute);
        }

        return region.Equals("us-east-1", StringComparison.OrdinalIgnoreCase)
            ? new Uri($"https://{options.Bucket.Trim()}.s3.amazonaws.com", UriKind.Absolute)
            : new Uri($"https://{options.Bucket.Trim()}.s3.{region}.amazonaws.com", UriKind.Absolute);
    }
}
