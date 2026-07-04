using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CapabilityCentricSample.Departments.Domain.ValueObjects;

public sealed class DepartmentName : ValueObject
{
    public string Value { get; }

    public DepartmentName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, DepartmentLocalizations.DEPARTMENT_NAME);

        var normalizedValue = value.TrimStart().TrimEnd();

        if (normalizedValue.Length > DepartmentConstants.DEPARTMENT_NAME_MAX_LENGTH
            || normalizedValue.Length < DepartmentConstants.DEPARTMENT_NAME_MIN_LENGTH)
        {
            throw new DomainException(
                SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                DepartmentLocalizations.DEPARTMENT_NAME,
                DepartmentConstants.DEPARTMENT_NAME_MIN_LENGTH,
                DepartmentConstants.DEPARTMENT_NAME_MAX_LENGTH);
        }

        Value = normalizedValue;
    }

    public static DepartmentName From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
