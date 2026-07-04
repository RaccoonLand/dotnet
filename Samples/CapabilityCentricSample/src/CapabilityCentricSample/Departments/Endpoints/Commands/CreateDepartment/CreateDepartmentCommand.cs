using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.CreateDepartment;

public sealed class CreateDepartmentCommand : ICommand<Guid>
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
}
