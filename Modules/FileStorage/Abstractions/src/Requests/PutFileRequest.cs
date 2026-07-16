namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Upload request.</summary>
public sealed class PutFileRequest
{
    /// <summary>When null or empty, the provider generates an opaque key.</summary>
    public string? Key { get; init; }

    public required Stream Content { get; init; }

    public string? ContentType { get; init; }

    /// <summary>Known content length. Strongly recommended for streaming uploads.</summary>
    public long? ContentLength { get; init; }

    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    public PutMode Mode { get; init; } = PutMode.CreateOnly;

    /// <summary>
    /// Optional per-upload content type allowlist. When set, providers enforce it in addition to
    /// <see cref="FileStorageOptions.AllowedContentTypes"/>. Usually populated by <see cref="FileStoragePutHelper"/>.
    /// </summary>
    public IReadOnlySet<string>? AllowedContentTypes { get; init; }

    /// <summary>
    /// Optional per-upload size limit. The effective limit is the minimum of this value and
    /// <see cref="FileStorageOptions.MaxUploadBytes"/>. Usually populated by <see cref="FileStoragePutHelper"/>.
    /// </summary>
    public long? MaxUploadBytes { get; init; }
}
