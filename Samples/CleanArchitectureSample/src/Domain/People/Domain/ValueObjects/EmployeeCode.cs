using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CleanArchitectureSample.People.Domain.ValueObjects;

public sealed class EmployeeCode : ValueObject
{
    public string Value { get; }

    public EmployeeCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.EMPLOYEE_CODE);

        string normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length > PersonConstants.EMPLOYEE_CODE_MAX_LENGTH || normalizedValue.Length < PersonConstants.EMPLOYEE_CODE_MIN_LENGTH)
            throw new DomainException(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.EMPLOYEE_CODE, PersonConstants.EMPLOYEE_CODE_MIN_LENGTH, PersonConstants.EMPLOYEE_CODE_MAX_LENGTH);

        Value = normalizedValue;
    }

    public static EmployeeCode From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
