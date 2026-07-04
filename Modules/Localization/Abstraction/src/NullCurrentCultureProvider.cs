using System.Globalization;

namespace RaccoonLand.Modules.MessageLocalization.Abstraction;

/// <summary>
/// Default <see cref="ICurrentCultureProvider"/> that never supplies a culture, so the implementation falls
/// back to its configured default. Any implementation package can register this as the default and let
/// consumers (for example an API reading a request header) replace it with their own provider.
/// </summary>
public sealed class NullCurrentCultureProvider : ICurrentCultureProvider
{
    /// <summary>A shared, stateless instance.</summary>
    public static readonly NullCurrentCultureProvider Instance = new();

    /// <inheritdoc />
    public CultureInfo? GetCurrentCulture() => null;
}
