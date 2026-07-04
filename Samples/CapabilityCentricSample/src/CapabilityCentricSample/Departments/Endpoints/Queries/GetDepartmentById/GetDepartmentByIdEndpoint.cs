using CapabilityCentricSample.Departments.Persistence.Queries;
using CapabilityCentricSample.Shared.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CapabilityCentricSample.Departments.Endpoints.Queries.GetDepartmentById;

public sealed class GetDepartmentByIdEndpoint(CapabilityCentricSampleQueryDbContext dbContext)
    : IEndpoint<GetDepartmentByIdQuery, GetDepartmentByIdResponse?>
{
    public async Task<Result<GetDepartmentByIdResponse?>> ExecuteAsync(
        GetDepartmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var department = await dbContext.Set<Department>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (department is null)
        {
            return Result<GetDepartmentByIdResponse?>.Success(null);
        }

        return Result<GetDepartmentByIdResponse?>.Success(new GetDepartmentByIdResponse
        {
            Id = department.Id,
            Code = department.Code,
            Name = department.Name,
            Description = department.Description,
            Status = department.Status,
            BusinessKey = department.BusinessKey,
            ConcurrencyToken = department.ConcurrencyToken,
        });
    }
}
