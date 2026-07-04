using System.Globalization;

namespace RaccoonLand.Modules.MessageLocalization.Abstraction;

/// <summary>
/// Resolves and formats user-facing messages at runtime. No message is hard-coded in the system; every
/// message template (and, where applicable, its parameters) can be changed at runtime by an administrator.
/// <para>
/// This package contains only the contract — like <c>IDistributedCache</c>. The actual resolution
/// (database, cache, resource files, ...) is provided by a separate implementation package.
/// </para>
/// </summary>
public interface IMessageLocalization
{
    /// <summary>
    /// Resolves <paramref name="messageTemplate"/> for the current culture and formats it with the
    /// optional <paramref name="parameters"/>.
    /// </summary>
    /// <param name="messageTemplate">The message template key (itself resolved by the implementation).</param>
    /// <param name="parameters">
    /// Optional values inserted into the template. By convention a <see cref="string"/> is treated as a
    /// localization key that the implementation resolves before insertion, while any non-string value is
    /// inserted as-is. To insert a literal string without resolution, wrap it with
    /// <see cref="RawValue.Raw(object?)"/>.
    /// </param>
    /// <example>
    /// <code>
    /// // "COMPANY" is resolved as a key; 10 and 60 are literals.
    /// localizer[ProjectValidationErrors.VALIDATION_ERROR_NOT_FOUND, ProjectTranslation.COMPANY];
    /// localizer[ProjectValidationErrors.VALIDATION_ERROR_AGE_BETWEEN, 10, 60];
    /// // Insert a literal string ("Raccoon") that must not be looked up as a key.
    /// localizer[ProjectValidationErrors.VALIDATION_ERROR_WELCOME, Raw("Raccoon")];
    /// </code>
    /// </example>
    string this[string messageTemplate, params object?[] parameters] { get; }

    /// <summary>
    /// Resolves <paramref name="messageTemplate"/> for the given <paramref name="culture"/> and formats it
    /// with the optional <paramref name="parameters"/>.
    /// </summary>
    /// <param name="culture">The target culture, typically built from a name such as <c>"fa-IR"</c>.</param>
    /// <param name="messageTemplate">The message template key (itself resolved by the implementation).</param>
    /// <param name="parameters">
    /// Optional values inserted into the template, following the same key/literal convention as the
    /// current-culture overload. Wrap a literal string with <see cref="RawValue.Raw(object?)"/>.
    /// </param>
    /// <example>
    /// <code>
    /// localizer[CultureInfo.GetCultureInfo("fa-IR"), ProjectValidationErrors.VALIDATION_ERROR_NOT_FOUND, ProjectTranslation.COMPANY];
    /// </code>
    /// </example>
    string this[CultureInfo culture, string messageTemplate, params object?[] parameters] { get; }
}
