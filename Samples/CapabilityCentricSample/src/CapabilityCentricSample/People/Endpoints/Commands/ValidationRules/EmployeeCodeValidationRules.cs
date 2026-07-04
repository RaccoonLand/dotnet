using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Commands.ValidationRules;

public static class EmployeeCodeValidationRules
{
    public static IRuleBuilderOptions<T, string> EmployeeCodeRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.EMPLOYEE_CODE])
            .Length(PersonConstants.EMPLOYEE_CODE_MIN_LENGTH, PersonConstants.EMPLOYEE_CODE_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization[SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.EMPLOYEE_CODE, PersonConstants.EMPLOYEE_CODE_MIN_LENGTH, PersonConstants.EMPLOYEE_CODE_MAX_LENGTH]);
    }
}
