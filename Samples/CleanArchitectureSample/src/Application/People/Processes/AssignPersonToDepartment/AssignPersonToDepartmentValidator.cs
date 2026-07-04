using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Processes.AssignPersonToDepartment;

public sealed class AssignPersonToDepartmentValidator : AbstractValidator<AssignPersonToDepartmentCommand>
{
    public AssignPersonToDepartmentValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.PersonId).IdRules(localization);
        RuleFor(x => x.DepartmentId)
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization[SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.ID]);
    }
}
