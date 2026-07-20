using System.Text.RegularExpressions;

namespace RaccoonLand.Modules.Messaging.Abstractions;

/// <summary>
/// Validates simple SQL Server identifiers used when building bracket-quoted names from options
/// (<c>Database</c>/<c>Schema</c>/<c>Table</c>). Rejects characters that would break out of
/// <c>[...]</c> quoting.
/// </summary>
public static partial class SqlIdentifier
{
    private const int MaxLength = 128;

    /// <summary>
    /// Returns <paramref name="value"/> when it is a non-empty simple identifier
    /// (<c>[A-Za-z_][A-Za-z0-9_]*</c>, max 128 chars); otherwise throws.
    /// </summary>
    public static string Require(string? value, string paramName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, paramName);

        if (value.Length > MaxLength || !IdentifierRegex().IsMatch(value))
        {
            throw new ArgumentException(
                $"{paramName} must be a simple SQL identifier ([A-Za-z_][A-Za-z0-9_]*, max {MaxLength} characters).",
                paramName);
        }

        return value;
    }

    /// <summary>Returns true when <paramref name="value"/> is a valid simple SQL identifier.</summary>
    public static bool IsValid(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= MaxLength
           && IdentifierRegex().IsMatch(value);

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.CultureInvariant)]
    private static partial Regex IdentifierRegex();
}
