using CleanArchitectureSample.Departments.Shared.Enums;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CleanArchitectureSample.Application.Departments.Queries.ReadModels;

public sealed class DepartmentReadModel : QueryAggregateRoot<Guid>
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DepartmentStatus Status { get; set; }
}
