using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Shared.ValidationRules;

public static class DescriptionValidationRules
{
    public static IRuleBuilderOptions<T, string?> DescriptionRules<T>(
        this IRuleBuilder<T, string?> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .MaximumLength(SharedConstants.DESCRIPTION_MAX_LENGTH)
            .WithErrorCode(SharedValidationMessageTemplates.STRING_LENGTH_LESS_THAN)
            .WithMessage(localization[
                SharedValidationMessageTemplates.STRING_LENGTH_LESS_THAN,
                SharedLocalizations.DESCRIPTION,
                SharedConstants.DESCRIPTION_MAX_LENGTH]);
    }
}
