using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.SetPersonPhoto;

public sealed class SetPersonPhotoValidator : AbstractValidator<SetPersonPhotoCommand>
{
    public SetPersonPhotoValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Id).IdRules(localization);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, "ContentType"));
        RuleFor(x => x.ContentLength)
            .GreaterThan(0)
            .WithErrorCode(SharedValidationMessageTemplates.NUMBER_GREATER_THAN)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.NUMBER_GREATER_THAN, "ContentLength"));
        RuleFor(x => x.Content)
            .Must(stream => stream is not null && stream != Stream.Null)
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, "Content"));
    }
}
