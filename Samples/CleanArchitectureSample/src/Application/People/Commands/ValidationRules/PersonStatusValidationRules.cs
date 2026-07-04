using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.People.Shared.Enums;
using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.ValidationRules;

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