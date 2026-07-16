namespace RaccoonLand.Modules.FileStorage.AspNetCore;

/// <summary>
/// Stable message template keys for FileStorage failures mapped by exception handlers.
/// Prefixed with <c>FILE_STORAGE_</c> so they do not collide with application keys.
/// Resolve with <c>IMessageLocalization</c>; register translations in your localization store.
/// </summary>
public static class FileStorageMessageTemplates
{
    /// <summary>Request failed validation (content type, size, key format, …).</summary>
    public const string VALIDATION_FAILED = "FILE_STORAGE_VALIDATION_FAILED";

    /// <summary>Object was not found.</summary>
    public const string FILE_NOT_FOUND = "FILE_STORAGE_FILE_NOT_FOUND";

    /// <summary>CreateOnly put when the key already exists.</summary>
    public const string FILE_ALREADY_EXISTS = "FILE_STORAGE_FILE_ALREADY_EXISTS";

    /// <summary>Provider denied access.</summary>
    public const string ACCESS_DENIED = "FILE_STORAGE_ACCESS_DENIED";

    /// <summary>Transient provider/network failure (retryable).</summary>
    public const string UNAVAILABLE = "FILE_STORAGE_UNAVAILABLE";

    /// <summary>Capability not supported by the active provider.</summary>
    public const string NOT_SUPPORTED = "FILE_STORAGE_NOT_SUPPORTED";

    /// <summary>Invalid or missing provider configuration.</summary>
    public const string CONFIGURATION_ERROR = "FILE_STORAGE_CONFIGURATION_ERROR";

    /// <summary>Fallback for unknown <see cref="RaccoonLand.Modules.FileStorage.Abstractions.FileStorageException"/> subtypes.</summary>
    public const string OPERATION_FAILED = "FILE_STORAGE_OPERATION_FAILED";
}
