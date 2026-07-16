namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Shared configuration for all file storage providers.</summary>
public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Maximum upload size in bytes. Null disables the limit.</summary>
    public long? MaxUploadBytes { get; set; }

    /// <summary>When set, uploads with other content types are rejected.</summary>
    public HashSet<string>? AllowedContentTypes { get; set; }

    /// <summary>Maximum number of user metadata entries per object.</summary>
    public int MaxMetadataEntries { get; set; } = 20;

    /// <summary>Maximum length of a single metadata value.</summary>
    public int MaxMetadataValueLength { get; set; } = 256;

    /// <summary>Default expiry for signed URLs when callers omit an explicit value.</summary>
    public TimeSpan DefaultSignedUrlExpiry { get; set; } = TimeSpan.FromMinutes(15);

    /// <summary>Upper bound for signed URL expiry requests.</summary>
    public TimeSpan MaxSignedUrlExpiry { get; set; } = TimeSpan.FromHours(24);
}
