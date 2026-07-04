namespace RaccoonLand.Modules.MessageLocalization.Abstraction;

/// <summary>
/// Wraps a parameter value that must be inserted into a message exactly as provided, bypassing the
/// implementation's key-resolution step.
/// <para>
/// The default convention for <see cref="IMessageLocalization"/> parameters is: a <see cref="string"/>
/// is a localization key and is resolved before insertion, while any non-string value is a literal.
/// A raw <see cref="string"/> (for example a person's name) would therefore be mistaken for a key —
/// wrapping it with <see cref="Raw(object?)"/> marks it as a literal instead.
/// </para>
/// </summary>
/// <example>
/// <code>
/// using static RaccoonLand.Modules.MessageLocalization.Abstraction.RawValue;
///
/// // "Raccoon" is inserted verbatim, not resolved as a key.
/// localizer[ProjectValidationErrors.VALIDATION_ERROR_WELCOME, Raw("Raccoon")];
/// </code>
/// </example>
public readonly struct RawValue
{
    private RawValue(object? value) => Value = value;

    /// <summary>The literal value to insert into the message without resolution.</summary>
    public object? Value { get; }

    /// <summary>Marks <paramref name="value"/> as a literal so the implementation inserts it as-is.</summary>
    public static RawValue Raw(object? value) => new(value);

    /// <inheritdoc />
    public override string? ToString() => Value?.ToString();
}
