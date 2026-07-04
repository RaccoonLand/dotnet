using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Departments.Domain.Entities;
using CleanArchitectureSample.Departments.Domain.ValueObjects;
using CleanArchitectureSample.Shared.ValueObjects;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CleanArchitectureSample.Application.Departments.Commands.CreateDepartment;

public sealed class CreateDepartmentEndpoint(ICommandDbContext db)
    : IEndpoint<CreateDepartmentCommand, Guid>
{
    public async Task<Result<Guid>> ExecuteAsync(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = Department.Create(
            DepartmentCode.From(request.Code),
            DepartmentName.From(request.Name),
            string.IsNullOrWhiteSpace(request.Description) ? null : Description.From(request.Description));

        db.Set<Department>().Add(department);
        await db.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.Id);
    }
}
