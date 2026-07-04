using System.Text.RegularExpressions;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;
using CapabilityCentricSample.Shared.Localizations;

namespace CapabilityCentricSample.Shared.ValueObjects;

public sealed partial class MobileNumber : ValueObject
{
    public string Value { get; }

    public MobileNumber(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.MOBILE_NUMBER);

        string normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length > SharedConstants.MOBILE_NUMBER_MAX_LENGTH || normalizedValue.Length < SharedConstants.MOBILE_NUMBER_MIN_LENGTH)
            throw new DomainException(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                SharedLocalizations.MOBILE_NUMBER, SharedConstants.MOBILE_NUMBER_MIN_LENGTH, SharedConstants.MOBILE_NUMBER_MAX_LENGTH );

        Value = normalizedValue;
    }

    public static MobileNumber From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}