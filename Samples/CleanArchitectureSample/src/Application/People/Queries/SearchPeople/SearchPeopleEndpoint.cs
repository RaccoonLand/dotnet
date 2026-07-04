using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Application.People.Queries.ReadModels;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.Persistence.SqlServer.Queries;

namespace CleanArchitectureSample.Application.People.Queries.SearchPeople;

public sealed class SearchPeopleEndpoint(IQueryDbContext db)
    : IEndpoint<SearchPeopleQuery, SearchPeopleResponse>
{
    public async Task<Result<SearchPeopleResponse>> ExecuteAsync(
        SearchPeopleQuery request,
        CancellationToken cancellationToken)
    {
        IQueryable<PersonReadModel> query = db.Set<PersonReadModel>();

        if (!string.IsNullOrWhiteSpace(request.EmployeeCode))
        {
            query = query.Where(x => x.EmployeeCode.Contains(request.EmployeeCode));
        }

        if (!string.IsNullOrWhiteSpace(request.FirstName))
        {
            query = query.Where(x => x.FirstName.Contains(request.FirstName));
        }

        if (!string.IsNullOrWhiteSpace(request.LastName))
        {
            query = query.Where(x => x.LastName.Contains(request.LastName));
        }

        if (request.Status is not null)
        {
            query = query.Where(x => x.Status == request.Status);
        }

        var paged = await query
            .OrderBy(x => x.LastName)
            .ThenBy(x => x.FirstName)
            .Select(x => new SearchPeopleItem
            {
                Id = x.Id,
                EmployeeCode = x.EmployeeCode,
                FirstName = x.FirstName,
                LastName = x.LastName,
                Status = x.Status,
            })
            .ToPagedListAsync(
                request.Page,
                request.PageSize,
                request.IncludeTotalCount,
                maxPageSize: 100,
                cancellationToken);

        return Result<SearchPeopleResponse>.Success(new SearchPeopleResponse
        {
            Items = paged.Items,
            TotalCount = paged.TotalCount,
            Page = paged.Page,
            PageSize = paged.PageSize,
        });
    }
}
