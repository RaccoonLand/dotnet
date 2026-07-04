using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.People.Processes.AssignPersonToDepartment;

public sealed record AssignPersonToDepartmentCommand : ICommand
{
    public int PersonId { get; init; }
    public Guid DepartmentId { get; init; }
}
