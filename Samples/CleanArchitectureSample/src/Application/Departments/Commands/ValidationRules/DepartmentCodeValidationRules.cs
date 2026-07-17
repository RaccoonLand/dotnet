using CleanArchitectureSample.Departments.Shared;
using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.Departments.Commands.ValidationRules;

public static class DepartmentCodeValidationRules
{
    public static IRuleBuilderOptions<T, string> DepartmentCodeRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, DepartmentLocalizations.DEPARTMENT_CODE))
            .Length(DepartmentConstants.DEPARTMENT_CODE_MIN_LENGTH, DepartmentConstants.DEPARTMENT_CODE_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                DepartmentLocalizations.DEPARTMENT_CODE,
                DepartmentConstants.DEPARTMENT_CODE_MIN_LENGTH,
                DepartmentConstants.DEPARTMENT_CODE_MAX_LENGTH));
    }
}
