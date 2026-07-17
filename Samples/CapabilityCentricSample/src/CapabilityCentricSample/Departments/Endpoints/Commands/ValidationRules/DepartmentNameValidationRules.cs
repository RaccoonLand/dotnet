using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.ValidationRules;

public static class DepartmentNameValidationRules
{
    public static IRuleBuilderOptions<T, string> DepartmentNameRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, DepartmentLocalizations.DEPARTMENT_NAME))
            .Length(DepartmentConstants.DEPARTMENT_NAME_MIN_LENGTH, DepartmentConstants.DEPARTMENT_NAME_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                DepartmentLocalizations.DEPARTMENT_NAME,
                DepartmentConstants.DEPARTMENT_NAME_MIN_LENGTH,
                DepartmentConstants.DEPARTMENT_NAME_MAX_LENGTH));
    }
}
