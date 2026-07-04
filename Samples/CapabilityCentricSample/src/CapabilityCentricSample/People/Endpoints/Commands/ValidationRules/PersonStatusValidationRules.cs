using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.People.Shared.Enums;
using CapabilityCentricSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Commands.ValidationRules;

public static class PersonStatusValidationRules
{
    public static IRuleBuilderOptions<T, PersonStatus> PersonStatusRules<T>(
        this IRuleBuilder<T, PersonStatus> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .IsInEnum()
            .WithErrorCode(SharedValidationMessageTemplates.ENUM_INVALID)
            .WithMessage(localization[SharedValidationMessageTemplates.ENUM_INVALID, PersonLocalizations.PERSON_STATUS]);
    }
}