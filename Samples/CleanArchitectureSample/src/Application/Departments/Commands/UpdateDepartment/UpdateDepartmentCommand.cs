using CleanArchitectureSample.Departments.Shared.Enums;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.Departments.Commands.UpdateDepartment;

public sealed record UpdateDepartmentCommand : ICommand
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DepartmentStatus Status { get; init; }
}
