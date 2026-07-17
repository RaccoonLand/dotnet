using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.ValidationRules;

public static class EmployeeCodeValidationRules
{
    public static IRuleBuilderOptions<T, string> EmployeeCodeRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.EMPLOYEE_CODE))
            .Length(PersonConstants.EMPLOYEE_CODE_MIN_LENGTH, PersonConstants.EMPLOYEE_CODE_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.EMPLOYEE_CODE, PersonConstants.EMPLOYEE_CODE_MIN_LENGTH, PersonConstants.EMPLOYEE_CODE_MAX_LENGTH));
    }
}
