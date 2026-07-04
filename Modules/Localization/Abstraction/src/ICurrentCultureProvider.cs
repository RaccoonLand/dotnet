using System.Globalization;

namespace RaccoonLand.Modules.MessageLocalization.Abstraction;

/// <summary>
/// Supplies the culture for the current operation when the caller does not pass one explicitly to
/// <see cref="IMessageLocalization"/>.
/// <para>
/// In an API this is typically implemented to read the culture from a request header (for example
/// <c>Accept-Language</c> or a custom header) via the current HTTP context. Returning <see langword="null"/>
/// lets the implementation fall back to its configured default culture.
/// </para>
/// </summary>
public interface ICurrentCultureProvider
{
    /// <summary>
    /// Returns the culture for the current operation, or <see langword="null"/> to use the configured default.
    /// </summary>
    CultureInfo? GetCurrentCulture();
}
