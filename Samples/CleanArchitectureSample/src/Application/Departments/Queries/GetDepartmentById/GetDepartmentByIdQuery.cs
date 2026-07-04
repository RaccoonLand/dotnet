using CleanArchitectureSample.Departments.Shared.Enums;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace CleanArchitectureSample.Application.Departments.Queries.GetDepartmentById;

public sealed class GetDepartmentByIdQuery : IQuery<GetDepartmentByIdResponse>, ICacheableRequest
{
    public Guid Id { get; init; }
    public string GetCacheKey() => $"{GetType().Name}:{Id}";
}

public sealed class GetDepartmentByIdResponse
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DepartmentStatus Status { get; init; }
    public Guid BusinessKey { get; init; }
    public Guid ConcurrencyToken { get; init; }
}
