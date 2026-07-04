using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentCommand : ICommand
{
    public Guid Id { get; init; }
}
