using CleanArchitectureSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CleanArchitectureSample.Application.People.Commands.DeletePerson;

public sealed class DeletePersonValidator : AbstractValidator<DeletePersonCommand>
{
    public DeletePersonValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Id).IdRules(localization);
    }
}
