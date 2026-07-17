using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Shared.ValidationRules;

public static class NationalCodeValidationRules
{
    public static IRuleBuilderOptions<T, string> NationalCodeRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.NATIONAL_CODE))
            .Length(SharedConstants.NATIONAL_CODE_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_EQUAL)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.STRING_LENGTH_EQUAL,
                SharedLocalizations.NATIONAL_CODE, SharedConstants.NATIONAL_CODE_LENGTH));
    }
}
