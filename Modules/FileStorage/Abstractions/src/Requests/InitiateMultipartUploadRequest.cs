namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Initiate multipart upload request.</summary>
public sealed class InitiateMultipartUploadRequest
{
    public string? Key { get; init; }

    public required string ContentType { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Optional per-upload content type allowlist. When set, providers enforce it in addition to
    /// <see cref="FileStorageOptions.AllowedContentTypes"/>. Usually populated by <see cref="FileStoragePutHelper"/>.
    /// </summary>
    public IReadOnlySet<string>? AllowedContentTypes { get; init; }

    /// <summary>
    /// Optional per-upload size limit for the completed object. The effective limit is the minimum of this value
    /// and <see cref="FileStorageOptions.MaxUploadBytes"/>.
    /// </summary>
    public long? MaxUploadBytes { get; init; }
}
