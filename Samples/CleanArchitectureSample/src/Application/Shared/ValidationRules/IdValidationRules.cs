using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Shared.ValidationRules;

public static class IdValidationRules
{
    public static IRuleBuilderOptions<T, int> IdRules<T>(
        this IRuleBuilder<T, int> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .GreaterThan(0)
            .WithErrorCode(SharedValidationMessageTemplates.NUMBER_GREATER_THAN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.NUMBER_GREATER_THAN, SharedLocalizations.ID));
    }
}
