namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Shared request validation helpers for file storage providers.</summary>
public static class FileStorageGuards
{
    public static void ValidatePutRequest(PutFileRequest request, FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Content);

        if (request.ContentLength is < 0)
        {
            throw new FileStorageValidationException("Content length cannot be negative.");
        }

        var effectiveMaxBytes = ResolveEffectiveMaxUploadBytes(request.MaxUploadBytes, options.MaxUploadBytes);

        if (effectiveMaxBytes is long maxBytes && request.ContentLength > maxBytes)
        {
            throw new FileStorageValidationException($"Upload exceeds the allowed limit of {maxBytes} bytes.");
        }

        ValidateContentType(request.ContentType, options.AllowedContentTypes);
        ValidateContentType(request.ContentType, request.AllowedContentTypes);

        ValidateMetadata(request.Metadata, options);
    }

    public static void ValidateSignedWriteRequest(SignedWriteUrlRequest request, FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateContentType(request.ContentType, options.AllowedContentTypes);
        ValidateContentType(request.ContentType, request.AllowedContentTypes);

        if (request.MaxUploadBytes is < 0)
        {
            throw new FileStorageValidationException("Max upload bytes cannot be negative.");
        }

        ValidateMaxUploadBytesLimit(request.MaxSizeBytes, options.MaxUploadBytes);
        ValidateMaxUploadBytesLimit(request.MaxUploadBytes, options.MaxUploadBytes);
    }

    public static void ValidateMultipartInitRequest(InitiateMultipartUploadRequest request, FileStorageOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateContentType(request.ContentType, options.AllowedContentTypes);
        ValidateContentType(request.ContentType, request.AllowedContentTypes);
        ValidateMaxUploadBytesLimit(request.MaxUploadBytes, options.MaxUploadBytes);
        ValidateMetadata(request.Metadata, options);
    }

    public static void ValidateMaxUploadBytesLimit(long? declaredLimit, long? optionsMaxUploadBytes)
    {
        if (declaredLimit is < 0)
        {
            throw new FileStorageValidationException("Max upload bytes cannot be negative.");
        }

        if (declaredLimit is long limit
            && optionsMaxUploadBytes is long globalMax
            && limit > globalMax)
        {
            throw new FileStorageValidationException(
                $"Max upload bytes cannot exceed the configured limit of {globalMax} bytes.");
        }
    }

    public static void ValidateContentType(string? contentType, IReadOnlySet<string>? allowedContentTypes)
    {
        if (allowedContentTypes is null or { Count: 0 })
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new FileStorageValidationException(
                "Content type is required when an allowlist is configured.");
        }

        if (!IsAllowedContentType(contentType, allowedContentTypes))
        {
            throw new FileStorageValidationException($"Content type '{contentType}' is not allowed.");
        }
    }

    public static long? ResolveEffectiveMaxUploadBytes(long? requestMaxUploadBytes, long? optionsMaxUploadBytes)
        => (requestMaxUploadBytes, optionsMaxUploadBytes) switch
        {
            (null, null) => null,
            (long requestMax, null) => requestMax,
            (null, long optionsMax) => optionsMax,
            (long requestMax, long optionsMax) => Math.Min(requestMax, optionsMax),
        };

    private static bool IsAllowedContentType(string contentType, IReadOnlySet<string> allowedContentTypes)
    {
        foreach (var allowed in allowedContentTypes)
        {
            if (string.Equals(allowed, contentType, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static void ValidateMetadata(IReadOnlyDictionary<string, string>? metadata, FileStorageOptions options)
    {
        if (metadata is null)
        {
            return;
        }

        if (metadata.Count > options.MaxMetadataEntries)
        {
            throw new FileStorageValidationException(
                $"Metadata cannot contain more than {options.MaxMetadataEntries} entries.");
        }

        foreach (var (key, value) in metadata)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new FileStorageValidationException("Metadata keys cannot be empty.");
            }

            if (value.Length > options.MaxMetadataValueLength)
            {
                throw new FileStorageValidationException(
                    $"Metadata value for '{key}' exceeds {options.MaxMetadataValueLength} characters.");
            }
        }
    }

    public static TimeSpan ResolveExpiry(TimeSpan? requestedExpiry, FileStorageOptions options)
    {
        var expiry = requestedExpiry ?? options.DefaultSignedUrlExpiry;

        if (expiry <= TimeSpan.Zero)
        {
            throw new FileStorageValidationException("Signed URL expiry must be greater than zero.");
        }

        if (expiry > options.MaxSignedUrlExpiry)
        {
            throw new FileStorageValidationException(
                $"Signed URL expiry cannot exceed {options.MaxSignedUrlExpiry}.");
        }

        return expiry;
    }
}
