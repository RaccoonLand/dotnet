using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.ValidationRules;

public static class LastNameValidationRules
{
    public static IRuleBuilderOptions<T, string> LastNameRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.LAST_NAME))
            .Length(PersonConstants.LAST_NAME_MIN_LENGTH, PersonConstants.LAST_NAME_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.LAST_NAME, PersonConstants.LAST_NAME_MIN_LENGTH, PersonConstants.LAST_NAME_MAX_LENGTH));
    }
}