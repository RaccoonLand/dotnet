using CapabilityCentricTemplate.People.Shared;
using CapabilityCentricTemplate.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricTemplate.People.Endpoints.Commands.ValidationRules;

public static class FirstNameValidationRules
{
    public static IRuleBuilderOptions<T, string> FirstNameRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, PersonLocalizations.FIRST_NAME])
            .Length(PersonConstants.FIRST_NAME_MIN_LENGTH, PersonConstants.FIRST_NAME_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization[SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                PersonLocalizations.FIRST_NAME, PersonConstants.FIRST_NAME_MIN_LENGTH, PersonConstants.FIRST_NAME_MAX_LENGTH]);
    }
}
