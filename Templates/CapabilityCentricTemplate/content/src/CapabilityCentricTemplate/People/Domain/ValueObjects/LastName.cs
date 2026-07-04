using CapabilityCentricTemplate.People.Shared;
using CapabilityCentricTemplate.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CapabilityCentricTemplate.People.Domain.ValueObjects;

public sealed class LastName : ValueObject
{
    public string Value { get; }

    public LastName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.LAST_NAME);

        string normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length > PersonConstants.LAST_NAME_MAX_LENGTH || normalizedValue.Length < PersonConstants.LAST_NAME_MIN_LENGTH)
            throw new DomainException(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.LAST_NAME, PersonConstants.LAST_NAME_MIN_LENGTH, PersonConstants.LAST_NAME_MAX_LENGTH);

        Value = normalizedValue;
    }

    public static LastName From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
