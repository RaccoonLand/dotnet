using CapabilityCentricSample.Shared.ValidationRules;
using FluentValidation;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Commands.DeletePerson;

public sealed class DeletePersonValidator : AbstractValidator<DeletePersonCommand>
{
    public DeletePersonValidator(IMessageLocalization localization)
    {
        RuleFor(x => x.Id).IdRules(localization);
    }
}
