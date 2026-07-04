using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Shared.ValidationRules;

public static class MobileNumberValidationRules
{
    public static IRuleBuilderOptions<T, string> MobileNumberRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.MOBILE_NUMBER])
            .Length(SharedConstants.MOBILE_NUMBER_MIN_LENGTH, SharedConstants.MOBILE_NUMBER_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization[SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                SharedLocalizations.MOBILE_NUMBER, SharedConstants.MOBILE_NUMBER_MIN_LENGTH, SharedConstants.MOBILE_NUMBER_MAX_LENGTH]);
    }
}