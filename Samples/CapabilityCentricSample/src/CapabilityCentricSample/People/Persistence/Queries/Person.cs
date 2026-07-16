using CapabilityCentricSample.People.Shared.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CapabilityCentricSample.People.Persistence.Queries;

public sealed class Person : QueryAggregateRoot<int>
{
    public string EmployeeCode { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string NationalCode { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MobileNumber { get; set; } = string.Empty;
    public DateTime EmploymentDate { get; set; }
    public PersonStatus Status { get; set; }
    public Guid? CurrentDepartmentId { get; set; }
    public DateTime? DepartmentAssignedAt { get; set; }
    public string? PhotoFileKey { get; set; }
    public string? ResumeFileKey { get; set; }
}
