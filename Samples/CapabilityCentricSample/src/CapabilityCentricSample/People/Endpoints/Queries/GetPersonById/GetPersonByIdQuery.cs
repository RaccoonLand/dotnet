using CapabilityCentricSample.People.Shared.Enums;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Modules.Middlewares.RequestCaching.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Queries.GetPersonById;

public sealed class GetPersonByIdQuery : IQuery<GetPersonByIdResponse>, ICacheableRequest
{
    public int Id { get; init; }
    public string GetCacheKey() => $"{GetType().Name}:{Id}";
}

public sealed class GetPersonByIdResponse
{
    public int Id { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string NationalCode { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string MobileNumber { get; init; } = string.Empty;
    public DateTime EmploymentDate { get; init; }
    public PersonStatus Status { get; init; }
    public Guid? CurrentDepartmentId { get; init; }
    public DateTime? DepartmentAssignedAt { get; init; }
    public string? PhotoFileKey { get; init; }
    public string? ResumeFileKey { get; init; }
}
