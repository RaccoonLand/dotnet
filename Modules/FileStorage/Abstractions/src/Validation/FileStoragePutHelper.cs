namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>
/// Application-facing helpers for building validated upload requests
/// without embedding storage validation rules in business code.
/// </summary>
public static class FileStoragePutHelper
{
    /// <summary>
    /// Validates the upload against per-operation constraints and optional global options,
    /// then returns a <see cref="PutFileRequest"/> ready for <see cref="IFileStorage.PutAsync"/>.
    /// </summary>
    public static PutFileRequest CreateRequest(
        Stream content,
        string contentType,
        FilePutConstraints constraints,
        PutMode mode = PutMode.CreateOnly,
        string? key = null,
        long? contentLength = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        FileStorageOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(constraints);

        var normalizedContentType = ValidateAndNormalizeContentType(contentType, constraints, options);

        var effectiveMaxBytes = FileStorageGuards.ResolveEffectiveMaxUploadBytes(
            constraints.MaxUploadBytes,
            options?.MaxUploadBytes);

        if (effectiveMaxBytes is long maxBytes && contentLength > maxBytes)
        {
            throw new FileStorageValidationException(
                $"Upload exceeds the allowed limit of {maxBytes} bytes.");
        }

        if (contentLength is < 0)
        {
            throw new FileStorageValidationException("Content length cannot be negative.");
        }

        if (options is not null)
        {
            FileStorageGuards.ValidateMetadata(metadata, options);
        }

        return new PutFileRequest
        {
            Key = key,
            Content = content,
            ContentType = normalizedContentType,
            ContentLength = contentLength,
            Metadata = metadata,
            Mode = mode,
            AllowedContentTypes = constraints.AllowedContentTypes,
            MaxUploadBytes = constraints.MaxUploadBytes,
        };
    }

    /// <summary>
    /// Validates and returns a <see cref="SignedWriteUrlRequest"/> for <see cref="IFileUrlSigner.GetWriteUrlAsync"/>.
    /// </summary>
    public static SignedWriteUrlRequest CreateSignedWriteRequest(
        string contentType,
        FilePutConstraints constraints,
        string? key = null,
        TimeSpan? expiry = null,
        long? maxSizeBytes = null,
        FileStorageOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        var normalizedContentType = ValidateAndNormalizeContentType(contentType, constraints, options);
        var effectiveMaxBytes = ResolveEffectiveMaxBytes(constraints, maxSizeBytes, options);

        if (options is not null)
        {
            FileStorageGuards.ValidateMaxUploadBytesLimit(constraints.MaxUploadBytes, options.MaxUploadBytes);
            FileStorageGuards.ValidateMaxUploadBytesLimit(maxSizeBytes, options.MaxUploadBytes);
        }

        return new SignedWriteUrlRequest
        {
            Key = key,
            ContentType = normalizedContentType,
            Expiry = expiry,
            MaxUploadBytes = constraints.MaxUploadBytes,
            MaxSizeBytes = maxSizeBytes ?? effectiveMaxBytes,
            AllowedContentTypes = constraints.AllowedContentTypes,
        };
    }

    /// <summary>
    /// Validates and returns an <see cref="InitiateMultipartUploadRequest"/> for
    /// <see cref="IMultipartUploadCoordinator.InitiateAsync"/>.
    /// </summary>
    public static InitiateMultipartUploadRequest CreateMultipartInitRequest(
        string contentType,
        FilePutConstraints constraints,
        string? key = null,
        IReadOnlyDictionary<string, string>? metadata = null,
        FileStorageOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(constraints);

        var normalizedContentType = ValidateAndNormalizeContentType(contentType, constraints, options);

        if (options is not null)
        {
            FileStorageGuards.ValidateMaxUploadBytesLimit(constraints.MaxUploadBytes, options.MaxUploadBytes);
            FileStorageGuards.ValidateMetadata(metadata, options);
        }

        return new InitiateMultipartUploadRequest
        {
            Key = key,
            ContentType = normalizedContentType,
            Metadata = metadata,
            AllowedContentTypes = constraints.AllowedContentTypes,
            MaxUploadBytes = constraints.MaxUploadBytes,
        };
    }

    private static string ValidateAndNormalizeContentType(
        string contentType,
        FilePutConstraints constraints,
        FileStorageOptions? options)
    {
        var normalizedContentType = NormalizeContentType(contentType);

        FileStorageGuards.ValidateContentType(normalizedContentType, constraints.AllowedContentTypes);
        FileStorageGuards.ValidateContentType(normalizedContentType, options?.AllowedContentTypes);

        return normalizedContentType;
    }

    private static long? ResolveEffectiveMaxBytes(
        FilePutConstraints constraints,
        long? explicitMaxBytes,
        FileStorageOptions? options)
        => FileStorageGuards.ResolveEffectiveMaxUploadBytes(
            FileStorageGuards.ResolveEffectiveMaxUploadBytes(constraints.MaxUploadBytes, explicitMaxBytes),
            options?.MaxUploadBytes);

    internal static string NormalizeContentType(string contentType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        return contentType.Trim();
    }
}
