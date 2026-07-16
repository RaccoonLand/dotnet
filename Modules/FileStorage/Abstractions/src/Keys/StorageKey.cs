using System.Text.RegularExpressions;

namespace RaccoonLand.Modules.FileStorage.Abstractions;

/// <summary>Normalizes and validates opaque storage keys.</summary>
public static partial class StorageKey
{
    public const int MaxLength = 128;
    public const int MinLength = 8;

    private const string Prefix = "file_";

    /// <summary>Generates a new opaque storage key.</summary>
    public static string Generate() => Prefix + Guid.CreateVersion7().ToString("N");

    /// <summary>Returns true when <paramref name="key"/> is a valid storage key.</summary>
    public static bool IsValid(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        var normalized = key.Trim();

        if (normalized.Length is < MinLength or > MaxLength)
        {
            return false;
        }

        return KeyFormat().IsMatch(normalized);
    }

    /// <summary>Validates and returns a trimmed key or throws <see cref="FileStorageValidationException"/>.</summary>
    public static string Normalize(string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var normalized = key.Trim();

        if (normalized.Length is < MinLength or > MaxLength)
        {
            throw new FileStorageValidationException(
                $"Storage key length must be between {MinLength} and {MaxLength}.");
        }

        if (!KeyFormat().IsMatch(normalized))
        {
            throw new FileStorageValidationException("Storage key contains invalid characters.");
        }

        if (normalized.Contains("..", StringComparison.Ordinal))
        {
            throw new FileStorageValidationException("Storage key must not contain path traversal sequences.");
        }

        return normalized;
    }

    /// <summary>Normalizes a caller-supplied key or generates a new one when null/empty.</summary>
    public static string NormalizeOrGenerate(string? key)
        => string.IsNullOrWhiteSpace(key) ? Generate() : Normalize(key);

    [GeneratedRegex("^[a-zA-Z0-9._-]+$")]
    private static partial Regex KeyFormat();
}
