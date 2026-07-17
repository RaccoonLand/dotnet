using CleanArchitectureSample.Application.Departments.Commands.ValidationRules;
using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithErrorCode(SharedValidationMessageTemplates.VALUE_REQUIRED)
            .WithMessage(localization.Get(SharedValidationMessageTemplates.VALUE_REQUIRED, SharedLocalizations.ID));

        RuleFor(x => x.Name).DepartmentNameRules(localization);
        RuleFor(x => x.Status).DepartmentStatusRules(localization);
        RuleFor(x => x.Description)
            .DescriptionRules(localization)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
