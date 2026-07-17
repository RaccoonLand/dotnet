using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Shared.ValidationRules;

public static class EmailValidationRules
{
    public static IRuleBuilderOptions<T, string> EmailRules<T>(
        this IRuleBuilder<T, string> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.EMAIL))
            .Length(SharedConstants.EMAIL_MIN_LENGTH, SharedConstants.EMAIL_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.STRING_LENGTH_BETWEEN,
                SharedLocalizations.EMAIL, SharedConstants.EMAIL_MIN_LENGTH, SharedConstants.EMAIL_MAX_LENGTH));
    }
}