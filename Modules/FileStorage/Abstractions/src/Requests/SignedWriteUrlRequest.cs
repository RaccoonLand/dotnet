namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Signed write URL request.</summary>
public sealed class SignedWriteUrlRequest
{
    public string? Key { get; init; }

    public required string ContentType { get; init; }

    public long? MaxSizeBytes { get; init; }

    /// <summary>
    /// Exact byte length the client will send on the signed PUT. Required by the S3 signer when a size
    /// limit is configured so <c>Content-Length</c> can be included in the signature.
    /// </summary>
    public long? ContentLength { get; init; }

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
