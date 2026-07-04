using CleanArchitectureSample.Application.Departments.Commands.ValidationRules;
using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Code).DepartmentCodeRules(localization);
        RuleFor(x => x.Name).DepartmentNameRules(localization);
        RuleFor(x => x.Description).DescriptionRules(localization)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
