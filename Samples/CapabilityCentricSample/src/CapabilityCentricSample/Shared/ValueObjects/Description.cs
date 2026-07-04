using CapabilityCentricSample.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CapabilityCentricSample.Shared.ValueObjects;

public sealed class Description : ValueObject
{
    public string Value { get; }

    public Description(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.DESCRIPTION);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > SharedConstants.DESCRIPTION_MAX_LENGTH)
        {
            throw new DomainException(
                SharedValidationMessageTemplates.STRING_LENGTH_LESS_THAN,
                SharedLocalizations.DESCRIPTION,
                SharedConstants.DESCRIPTION_MAX_LENGTH);
        }

        Value = normalizedValue;
    }

    public static Description From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
