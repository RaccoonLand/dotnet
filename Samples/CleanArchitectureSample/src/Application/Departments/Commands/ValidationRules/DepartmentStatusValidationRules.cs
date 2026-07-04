using CleanArchitectureSample.Departments.Shared;
using CleanArchitectureSample.Departments.Shared.Enums;
using CleanArchitectureSample.Shared.Localizations;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.Departments.Commands.ValidationRules;

public static class DepartmentStatusValidationRules
{
    public static IRuleBuilderOptions<T, DepartmentStatus> DepartmentStatusRules<T>(
        this IRuleBuilder<T, DepartmentStatus> ruleBuilder,
        IMessageLocalization localization)
    {
        return ruleBuilder
            .IsInEnum()
            .WithErrorCode(SharedValidationMessageTemplates.ENUM_INVALID)
            .WithMessage(localization[SharedValidationMessageTemplates.ENUM_INVALID, DepartmentLocalizations.DEPARTMENT_STATUS]);
    }
}
