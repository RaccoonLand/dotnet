using CapabilityCentricSample.Departments.Domain.ValueObjects;
using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Departments.Shared.Enums;
using CapabilityCentricSample.Shared.ValueObjects;
using RaccoonLand.Core.Domain.Abstractions;

namespace CapabilityCentricSample.Departments.Domain.Entities;

public sealed class Department : AggregateRoot<Guid>
{
    public DepartmentCode Code { get; private set; } = null!;
    public DepartmentName Name { get; private set; } = null!;
    public Description? Description { get; private set; }
    public DepartmentStatus Status { get; private set; }

    private Department()
    {
    }

    private Department(Guid id)
        : base(id)
    {
    }

    public static Department Create(
        DepartmentCode code,
        DepartmentName name,
        Description? description)
    {
        return new Department(Guid.CreateVersion7())
        {
            Code = code,
            Name = name,
            Description = description,
            Status = DepartmentStatus.Active,
        };
    }

    public void Update(
        DepartmentName name,
        Description? description,
        DepartmentStatus status)
    {
        Name = name;
        Description = description;
        Status = status;
    }
}
