using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricTemplate.People.Endpoints.Commands.CreatePerson;

public sealed class CreatePersonCommand : ICommand<int>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
