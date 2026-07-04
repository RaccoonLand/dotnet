using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Application.Departments.Queries.ReadModels;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;

namespace CleanArchitectureSample.Application.Departments.Queries.GetDepartmentById;

public sealed class GetDepartmentByIdEndpoint(IQueryDbContext db)
    : IEndpoint<GetDepartmentByIdQuery, GetDepartmentByIdResponse?>
{
    public async Task<Result<GetDepartmentByIdResponse?>> ExecuteAsync(
        GetDepartmentByIdQuery request,
        CancellationToken cancellationToken)
    {
        var department = await db.Set<DepartmentReadModel>()
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
