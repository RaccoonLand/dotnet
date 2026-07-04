using CapabilityCentricSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.Shared.ValidationRules;

public static class EmailValidationRules
{
    public static IRuleBuilderOptions<T, string> EmailRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.EMAIL])
            .Length(SharedConstants.EMAIL_MIN_LENGTH, SharedConstants.EMAIL_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization[SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                SharedLocalizations.EMAIL, SharedConstants.EMAIL_MIN_LENGTH, SharedConstants.EMAIL_MAX_LENGTH]);
    }
}