using CleanArchitectureSample.People.Domain.ValueObjects;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.People.Shared.Enums;
using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValueObjects;
using RaccoonLand.Core.Domain.Abstractions;
using RaccoonLand.Core.Domain.Exceptions;

namespace CleanArchitectureSample.People.Domain.Entities;

public sealed class Person : AggregateRoot<int>
{
    #region Properties
    public EmployeeCode EmployeeCode { get; private set; } = null!;
    public FirstName FirstName { get; private set; } = null!;
    public LastName LastName { get; private set; } = null!;
    public NationalCode NationalCode { get; private set; } = null!;
    public Email Email { get; private set; } = null!;
    public MobileNumber MobileNumber { get; private set; } = null!;
    public DateTime EmploymentDate { get; private set; }
    public PersonStatus Status { get; private set; }
    public Guid? CurrentDepartmentId { get; private set; }
    public DateTime? DepartmentAssignedAt { get; private set; }

    /// <summary>Opaque FileStorage key for the person photo. Persist only the key, never a path or URL.</summary>
    public string? PhotoFileKey { get; private set; }

    /// <summary>Opaque FileStorage key for the person resume PDF. Persist only the key, never a path or URL.</summary>
    public string? ResumeFileKey { get; private set; }

    #endregion

    #region Constructors

    private Person()
    {
    }

    #endregion

    #region Business

    public static Person Create(
        EmployeeCode employeeCode,
        FirstName firstName,
        LastName lastName,
        NationalCode nationalCode,
        Email email,
        MobileNumber mobileNumber,
        DateTime employmentDate)
    {
        return new Person
        {
            EmployeeCode = employeeCode,
            FirstName = firstName,
            LastName = lastName,
            NationalCode = nationalCode,
            Email = email,
            MobileNumber = mobileNumber,
            EmploymentDate = employmentDate,
            Status = PersonStatus.Active,
        };
    }

    public void Update(
        FirstName firstName,
        LastName lastName,
        Email email,
        MobileNumber mobileNumber,
        PersonStatus status)
    {
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        MobileNumber = mobileNumber;
        Status = status;
    }

    public void AssignToDepartment(Guid departmentId)
    {
        if (Status != PersonStatus.Active)
            throw new DomainException(SharedBusinessMessageTemplates.ENTITY_IS_NOT_ACTIVE, PersonLocalizations.PERSON);

        CurrentDepartmentId = departmentId;
        DepartmentAssignedAt = DateTime.UtcNow;
    }

    public void SetPhotoFileKey(string fileKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileKey);
        PhotoFileKey = fileKey;
    }

    public void SetResumeFileKey(string fileKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileKey);
        ResumeFileKey = fileKey;
    }

    #endregion
}
