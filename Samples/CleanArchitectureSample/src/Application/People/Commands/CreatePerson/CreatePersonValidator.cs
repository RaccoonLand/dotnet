using CleanArchitectureSample.Application.People.Commands.ValidationRules;
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
    }
}
