namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Signed write URL request.</summary>
public sealed class SignedWriteUrlRequest
{
    public string? Key { get; init; }

    public required string ContentType { get; init; }

    public long? MaxSizeBytes { get; init; }

    public TimeSpan? Expiry { get; init; }

    /// <summary>
    /// Optional per-upload content type allowlist. When set, providers enforce it in addition to
    /// <see cref="FileStorageOptions.AllowedContentTypes"/>. Usually populated by <see cref="FileStoragePutHelper"/>.
    /// </summary>
    public IReadOnlySet<string>? AllowedContentTypes { get; init; }

    /// <summary>
    /// Optional per-upload size limit used during validation. The effective limit is the minimum of this value,
    /// <see cref="MaxSizeBytes"/>, and <see cref="FileStorageOptions.MaxUploadBytes"/>.
    /// </summary>
    public long? MaxUploadBytes { get; init; }
}
