namespace RaccoonLand.Modules.MessageLocalization.Abstraction;

/// <summary>
/// Wraps a <see cref="string"/> that must be inserted into a message exactly as provided, bypassing the
/// implementation's key-resolution step.
/// <para>
/// The default convention for <see cref="IMessageLocalization"/> parameters is: a <see cref="string"/>
/// is a localization key and is resolved before insertion, while any non-string value is a literal.
/// A string that must appear verbatim (for example a person's name) would therefore be mistaken for a key —
/// wrapping it with <see cref="Raw(string?)"/> marks it as a literal instead.
/// </para>
/// <para>
/// Only strings need this escape hatch. Numbers, dates, and other non-string values are already literals.
/// <see cref="Raw(string?)"/> accepts <see langword="null"/>; implementations insert that as an empty
/// literal (<see cref="string.Empty"/>), same as a bare <see langword="null"/> parameter element.
/// </para>
/// </summary>
/// <example>
/// <code>
/// using static RaccoonLand.Modules.MessageLocalization.Abstraction.RawValue;
///
/// // "Raccoon" is inserted verbatim, not resolved as a key.
/// localizer.Get(ProjectValidationErrors.VALIDATION_ERROR_WELCOME, Raw("Raccoon"));
/// </code>
/// </example>
public readonly struct RawValue
{
    private RawValue(string? value) => Value = value;

    /// <summary>The literal string to insert into the message without resolution.</summary>
    public string? Value { get; }

    /// <summary>
    /// Marks <paramref name="value"/> as a literal string so the implementation inserts it as-is.
    /// When <paramref name="value"/> is <see langword="null"/>, the inserted text is empty.
    /// </summary>
    public static RawValue Raw(string? value) => new(value);

    /// <inheritdoc />
    public override string ToString() => Value ?? string.Empty;
}
