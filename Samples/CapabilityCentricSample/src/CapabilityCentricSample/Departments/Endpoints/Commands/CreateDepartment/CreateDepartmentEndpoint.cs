using CapabilityCentricSample.Departments.Domain.Entities;
using CapabilityCentricSample.Departments.Domain.ValueObjects;
using CapabilityCentricSample.Shared.Persistence.Commands;
using CapabilityCentricSample.Shared.ValueObjects;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.CreateDepartment;

public sealed class CreateDepartmentEndpoint(CapabilityCentricSampleCommandDbContext dbContext)
    : IEndpoint<CreateDepartmentCommand, Guid>
{
    public async Task<Result<Guid>> ExecuteAsync(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = Department.Create(
            DepartmentCode.From(request.Code),
            DepartmentName.From(request.Name),
            string.IsNullOrWhiteSpace(request.Description) ? null : Description.From(request.Description));

        dbContext.Set<Department>().Add(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.Id);
    }
}
