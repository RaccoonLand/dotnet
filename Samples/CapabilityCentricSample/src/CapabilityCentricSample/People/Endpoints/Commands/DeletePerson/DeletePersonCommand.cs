using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.People.Endpoints.Commands.DeletePerson;

public sealed class DeletePersonCommand : ICommand
{
    public int Id { get; init; }
}
