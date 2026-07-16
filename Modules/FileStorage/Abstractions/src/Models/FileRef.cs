namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>
/// Provider-neutral reference to a stored file. Persist this in application databases instead of
/// provider-specific paths, buckets, or connection details.
/// </summary>
public sealed record FileRef
{
    /// <summary>Opaque storage key assigned by the active provider.</summary>
    public required string Key { get; init; }

    /// <summary>Provider-specific version token (for example ETag or row version).</summary>
    public string? Version { get; init; }

    /// <summary>Stored content length in bytes.</summary>
    public long? Length { get; init; }

    /// <summary>MIME content type when known.</summary>
    public string? ContentType { get; init; }

    /// <summary>Lowercase hexadecimal SHA-256 checksum when computed by the provider.</summary>
    public string? ChecksumSha256 { get; init; }

    /// <summary>UTC timestamp when the object was created.</summary>
    public DateTimeOffset? CreatedAtUtc { get; init; }
}
