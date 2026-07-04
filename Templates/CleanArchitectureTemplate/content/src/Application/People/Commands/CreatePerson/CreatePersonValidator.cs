using CleanArchitectureTemplate.Application.People.Commands.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureTemplate.Application.People.Commands.CreatePerson;

public sealed class CreatePersonValidator : AbstractValidator<CreatePersonCommand>
{
    public CreatePersonValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.FirstName).FirstNameRules(localization);
        RuleFor(x => x.LastName).LastNameRules(localization);
    }
}
