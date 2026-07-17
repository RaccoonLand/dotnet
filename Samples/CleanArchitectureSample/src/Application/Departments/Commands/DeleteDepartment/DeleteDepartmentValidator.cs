using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentValidator : AbstractValidator<DeleteDepartmentCommand>
{
    public DeleteDepartmentValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.ID));
    }
}
