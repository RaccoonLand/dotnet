using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.DeleteDepartment;

public sealed class DeleteDepartmentCommand : ICommand
{
    public Guid Id { get; init; }
}
