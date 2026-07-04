using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureTemplate.Application.People.Commands.CreatePerson;

public sealed class CreatePersonCommand : ICommand<int>
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
