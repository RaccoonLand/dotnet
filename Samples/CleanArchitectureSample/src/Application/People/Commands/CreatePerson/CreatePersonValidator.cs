using CleanArchitectureSample.Application.People.Commands.ValidationRules;
using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.CreatePerson;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.EmployeeCode).EmployeeCodeRules(localization);
        RuleFor(x => x.FirstName).FirstNameRules(localization);
        RuleFor(x => x.LastName).LastNameRules(localization);
        RuleFor(x => x.NationalCode).NationalCodeRules(localization);
        RuleFor(x => x.Email).EmailRules(localization);
        RuleFor(x => x.MobileNumber).MobileNumberRules(localization);

        RuleFor(x => x.PhotoContentType)
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, "PhotoContentType"]);
        RuleFor(x => x.PhotoContentLength)
            .GreaterThan(0)
            .WithErrorCode(SharedValidationMessageTemplates.NUMBER_GREATER_THAN)
            .WithMessage(localization[SharedValidationMessageTemplates.NUMBER_GREATER_THAN, "PhotoContentLength"]);
        RuleFor(x => x.PhotoContent)
            .Must(stream => stream is not null && stream != Stream.Null)
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, "PhotoContent"]);

        RuleFor(x => x.ResumeContentType)
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, "ResumeContentType"]);
        RuleFor(x => x.ResumeContentLength)
            .GreaterThan(0)
            .WithErrorCode(SharedValidationMessageTemplates.NUMBER_GREATER_THAN)
            .WithMessage(localization[SharedValidationMessageTemplates.NUMBER_GREATER_THAN, "ResumeContentLength"]);
        RuleFor(x => x.ResumeContent)
            .Must(stream => stream is not null && stream != Stream.Null)
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, "ResumeContent"]);
    }
}
