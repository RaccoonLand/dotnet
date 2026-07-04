using CleanArchitectureSample.Application.People.Commands.ValidationRules;
using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.UpdatePerson;

public sealed class UpdatePersonValidator : AbstractValidator<UpdatePersonCommand>
{
    public UpdatePersonValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Id).IdRules(localization);
        RuleFor(x => x.FirstName).FirstNameRules(localization);
        RuleFor(x => x.LastName).LastNameRules(localization);
        RuleFor(x => x.MobileNumber).MobileNumberRules(localization);
        RuleFor(x => x.Status).PersonStatusRules(localization);
    }
}
