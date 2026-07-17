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
    /// <param name="messageTemplate">
    /// The message template key. Must not be <see langword="null"/>.
    /// </param>
    /// <param name="parameters">
    /// Optional values inserted into the template. Must not be a <see langword="null"/> array (omit the
    /// argument or pass an empty array). By convention a <see cref="string"/> is treated as a
    /// localization key that the implementation resolves before insertion, while any non-string value is
    /// inserted as-is. A <see langword="null"/> element or <see cref="RawValue.Raw(string?)"/> with a
    /// <see langword="null"/> value is inserted as an empty literal (same as <see cref="string.Empty"/>).
    /// To insert a literal string without key resolution, wrap it with <see cref="RawValue.Raw(string?)"/>.
    /// Only strings need <see cref="RawValue"/>; numbers and other non-strings are already literals.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="messageTemplate"/> is <see langword="null"/>, or <paramref name="parameters"/> is a
    /// <see langword="null"/> array.
    /// </exception>
    /// <example>
    /// <code>
    /// // "COMPANY" is resolved as a key; 10 and 60 are literals.
    /// localizer.Get(ProjectValidationErrors.VALIDATION_ERROR_NOT_FOUND, ProjectTranslation.COMPANY);
    /// localizer.Get(ProjectValidationErrors.VALIDATION_ERROR_AGE_BETWEEN, 10, 60);
    /// // Insert a literal string ("Raccoon") that must not be looked up as a key.
    /// localizer.Get(ProjectValidationErrors.VALIDATION_ERROR_WELCOME, Raw("Raccoon"));
    /// </code>
    /// </example>
    string Get(string messageTemplate, params object?[] parameters);

    /// <summary>
    /// Resolves <paramref name="messageTemplate"/> for the given <paramref name="culture"/> and formats it
    /// with the optional <paramref name="parameters"/>.
    /// </summary>
    /// <param name="culture">
    /// The target culture (for example from <c>CultureInfo.GetCultureInfo("fa-IR")</c>).
    /// Must not be <see langword="null"/>. Unlike <see cref="ICurrentCultureProvider.GetCurrentCulture"/>,
    /// <see langword="null"/> is not a fallback signal here — use <see cref="Get(string, object?[])"/> instead.
    /// </param>
    /// <param name="messageTemplate">The message template key. Must not be <see langword="null"/>.</param>
    /// <param name="parameters">
    /// Optional values inserted into the template, following the same key/literal/<see langword="null"/>
    /// convention as <see cref="Get(string, object?[])"/>. Must not be a <see langword="null"/> array.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="culture"/>, <paramref name="messageTemplate"/>, or the <paramref name="parameters"/>
    /// array is <see langword="null"/>.
    /// </exception>
    /// <example>
    /// <code>
    /// localizer.GetForCulture(
    ///     CultureInfo.GetCultureInfo("fa-IR"),
    ///     ProjectValidationErrors.VALIDATION_ERROR_NOT_FOUND,
    ///     ProjectTranslation.COMPANY);
    /// </code>
    /// </example>
    string GetForCulture(CultureInfo culture, string messageTemplate, params object?[] parameters);
}
