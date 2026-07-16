namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>
/// Per-operation upload guardrails supplied by the application.
/// Use with <see cref="FileStoragePutHelper"/> to keep endpoint code free of storage details.
/// </summary>
public sealed record FilePutConstraints
{
    /// <summary>Content types permitted for this upload operation.</summary>
    public required IReadOnlySet<string> AllowedContentTypes { get; init; }

    /// <summary>Optional size limit for this operation. The effective limit is the minimum of this value and <see cref="FileStorageOptions.MaxUploadBytes"/>.</summary>
    public long? MaxUploadBytes { get; init; }

    /// <summary>Creates constraints for the given MIME types (comparison is case-insensitive).</summary>
    public static FilePutConstraints For(params string[] contentTypes)
    {
        if (contentTypes is null or { Length: 0 })
        {
            throw new ArgumentException("At least one content type is required.", nameof(contentTypes));
        }

        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var contentType in contentTypes)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
            set.Add(contentType.Trim());
        }

        return new FilePutConstraints { AllowedContentTypes = set };
    }

    /// <summary>Common image upload profile.</summary>
    public static FilePutConstraints Images { get; } = For(
        "image/jpeg",
        "image/png",
        "image/webp");

    /// <summary>PDF documents only.</summary>
    public static FilePutConstraints PdfDocuments { get; } = For("application/pdf");
}
