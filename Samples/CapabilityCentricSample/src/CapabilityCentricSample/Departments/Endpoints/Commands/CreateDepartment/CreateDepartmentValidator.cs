using CapabilityCentricSample.Departments.Endpoints.Commands.ValidationRules;
using CapabilityCentricSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.CreateDepartment;

public sealed class CreateDepartmentValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Code).DepartmentCodeRules(localization);
        RuleFor(x => x.Name).DepartmentNameRules(localization);
        RuleFor(x => x.Description)
            .DescriptionRules(localization)
            .When(x => !string.IsNullOrWhiteSpace(x.Description));
    }
}
