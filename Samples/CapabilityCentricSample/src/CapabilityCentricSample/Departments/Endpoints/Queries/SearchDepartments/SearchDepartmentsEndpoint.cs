using CapabilityCentricSample.Departments.Persistence.Queries;
using CapabilityCentricSample.Shared.Persistence.Queries;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CapabilityCentricSample.Departments.Endpoints.Queries.SearchDepartments;

public sealed class SearchDepartmentsEndpoint(CapabilityCentricSampleQueryDbContext dbContext)
    : IEndpoint<SearchDepartmentsQuery, SearchDepartmentsResponse>
{
    public async Task<Result<SearchDepartmentsResponse>> ExecuteAsync(
        SearchDepartmentsQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<Department> query = dbContext.Set<Department>();

        if (!string.IsNullOrWhiteSpace(request.Code))
        {
            query = query.Where(x => x.Code.Contains(request.Code));
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            query = query.Where(x => x.Name.Contains(request.Name));
        }

        if (request.Status is not null)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        var paged = await query
            .OrderBy(x => x.Name)
            .Select(x => new SearchDepartmentsItem
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                Status = x.Status,
            })
            .ToPagedListAsync(
                request.Page,
                request.PageSize,
                request.IncludeTotalCount,
                maxPageSize: 100,
                cancellationToken);

        return Result<SearchDepartmentsResponse>.Success(new SearchDepartmentsResponse
        {
            Items = paged.Items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize,
        });
    }
}
