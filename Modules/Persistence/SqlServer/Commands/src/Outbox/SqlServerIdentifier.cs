namespace RaccoonLand.Modules.Persistence.SqlServer.Commands.Outbox;

/// <summary>
/// SQL Server identifier hardening for values that reach raw SQL via string interpolation
/// (outbox <c>INSERT</c> statements). The outbox writers do not use parameterized identifiers;
/// misconfigured or attacker-influenced options must not be able to break out of a bracket-quoted
/// identifier.
/// </summary>
/// <remarks>
/// Two guarantees are applied at composition time (registration / options validation):
/// <list type="bullet">
///   <item><description>The identifier body matches the T-SQL regular-identifier grammar
///     (<c>[A-Za-z_][A-Za-z0-9_]{0,127}</c>). Multi-part identifiers, spaces, dots and quotes
///     are rejected up-front so the render never has to escape them.</description></item>
///   <item><description>When emitted, any embedded <c>]</c> is doubled to <c>]]</c> per T-SQL
///     bracket-quoting rules as defence in depth. Given the regex above, this normally is a no-op,
///     but keeps the emitter safe if the input rules ever loosen.</description></item>
/// </list>
/// </remarks>
internal static class SqlServerIdentifier
{
    private static readonly System.Text.RegularExpressions.Regex Pattern = new(
        "^[A-Za-z_][A-Za-z0-9_]{0,127}$",
        System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    /// <summary>
    /// Throws when <paramref name="identifier"/> is not a valid single-part T-SQL regular identifier.
    /// Empty, whitespace, dotted, quoted and out-of-range values are all rejected.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// <paramref name="identifier"/> is null, empty, whitespace, or not a valid identifier.
    /// </exception>
    public static void Validate(string? identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException(
                $"SQL Server identifier '{parameterName}' is required and cannot be null, empty, or whitespace.",
                parameterName);
        }

        if (!Pattern.IsMatch(identifier))
        {
            throw new ArgumentException(
                $"SQL Server identifier '{parameterName}' = '{identifier}' is not a valid single-part identifier. " +
                "Expected: starts with a letter or underscore, contains only letters, digits, or underscores, " +
                "length 1-128. Multi-part names (e.g. 'schema.table'), quoted names, or names with spaces are not allowed.",
                parameterName);
        }
    }

    /// <summary>
    /// Renders <paramref name="identifier"/> as a bracket-quoted part (<c>[identifier]</c>), doubling any
    /// embedded <c>]</c> per T-SQL rules. Assumes the caller has already run <see cref="Validate"/>.
    /// </summary>
    public static string QuotePart(string identifier)
        => $"[{identifier.Replace("]", "]]")}]";
}
