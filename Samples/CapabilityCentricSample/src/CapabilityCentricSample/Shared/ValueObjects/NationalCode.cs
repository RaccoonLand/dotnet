using System.Text.RegularExpressions;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;
using CapabilityCentricSample.Shared.Localizations;

namespace CapabilityCentricSample.People.Domain.ValueObjects;

public sealed partial class NationalCode : ValueObject
{
    public string Value { get; }

    public NationalCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.NATIONAL_CODE);

        string normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length != SharedConstants.NATIONAL_CODE_LENGTH)
            throw new DomainException(SharedValidationMessageTemplates.STRING_LENGTH_EQUAL,
                SharedLocalizations.NATIONAL_CODE, SharedConstants.NATIONAL_CODE_LENGTH);

        if (!NationalCodeFormat().IsMatch(normalizedValue))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_INVALID, SharedLocalizations.NATIONAL_CODE);

        Value = normalizedValue;
    }

    public static NationalCode From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    [GeneratedRegex(@"^\d{10}$")]
    private static partial Regex NationalCodeFormat();
}
