using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Shared.Localizations;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CapabilityCentricSample.Departments.Domain.ValueObjects;

public sealed class DepartmentCode : ValueObject
{
    public string Value { get; }

    public DepartmentCode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException(SharedValidationMessageTemplates.VALUE_REQUIRED, DepartmentLocalizations.DEPARTMENT_CODE);

        var normalizedValue = value.Trim();

        if (normalizedValue.Length > DepartmentConstants.DEPARTMENT_CODE_MAX_LENGTH
            || normalizedValue.Length < DepartmentConstants.DEPARTMENT_CODE_MIN_LENGTH)
        {
            throw new DomainException(
                SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                DepartmentLocalizations.DEPARTMENT_CODE,
                DepartmentConstants.DEPARTMENT_CODE_MIN_LENGTH,
                DepartmentConstants.DEPARTMENT_CODE_MAX_LENGTH);
        }

        Value = normalizedValue;
    }

    public static DepartmentCode From(string value) => new(value);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
