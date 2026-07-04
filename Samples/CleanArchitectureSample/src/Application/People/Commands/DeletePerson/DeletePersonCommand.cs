using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.People.Commands.DeletePerson;

public sealed class DeletePersonCommand : ICommand
{
    public int Id { get; init; }
}
