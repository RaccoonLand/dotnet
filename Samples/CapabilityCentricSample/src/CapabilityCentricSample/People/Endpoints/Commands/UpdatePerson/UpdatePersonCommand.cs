using CapabilityCentricSample.People.Shared.Enums;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.People.Endpoints.Commands.UpdatePerson;

public sealed record UpdatePersonCommand : ICommand
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public PersonStatus Status { get; init; }
}
