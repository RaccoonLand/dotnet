using CapabilityCentricSample.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace CapabilityCentricSample.People.Domain.ValueObjects;

public sealed partial class Email : ValueObject
{
    public string Value { get; }

    public Email(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.EMAIL);

        string normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length > SharedConstants.EMAIL_MAX_LENGTH || normalizedValue.Length < SharedConstants.EMAIL_MIN_LENGTH)
            throw new DomainException(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                SharedLocalizations.EMAIL, SharedConstants.EMAIL_MIN_LENGTH, SharedConstants.EMAIL_MAX_LENGTH);

        if (!EmailFormat().IsMatch(normalizedValue))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_INVALID, SharedLocalizations.EMAIL);

        Value = normalizedValue;
    }

    public static Email From(string value) => new(value);

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailFormat();

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
