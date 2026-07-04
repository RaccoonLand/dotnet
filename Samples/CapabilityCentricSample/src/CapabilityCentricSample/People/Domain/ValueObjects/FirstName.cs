using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CapabilityCentricSample.People.Domain.ValueObjects;

public sealed class FirstName : ValueObject
{
    public const int MaxLength = 100;

    public string Value { get; }

    public FirstName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.FIRST_NAME);

        string normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length > PersonConstants.FIRST_NAME_MAX_LENGTH || normalizedValue.Length < PersonConstants.FIRST_NAME_MIN_LENGTH)
            throw new DomainException(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.FIRST_NAME, PersonConstants.FIRST_NAME_MIN_LENGTH, PersonConstants.FIRST_NAME_MAX_LENGTH);

        Value = normalizedValue;
    }

    public static FirstName From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
