using CleanArchitectureSample.Departments.Shared.Enums;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CleanArchitectureSample.Application.Departments.Queries.SearchDepartments;

public sealed class SearchDepartmentsQuery : PagedQuery<SearchDepartmentsResponse>
{
    public string? Code { get; init; }
    public string? Name { get; init; }
    public DepartmentStatus? Status { get; init; }
}

public sealed record SearchDepartmentsResponse : PageResponse<SearchDepartmentsItem>;

public sealed class SearchDepartmentsItem
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public DepartmentStatus Status { get; init; }
}
