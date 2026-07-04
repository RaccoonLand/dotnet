using CapabilityCentricSample.People.Shared.Enums;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;

namespace CapabilityCentricSample.People.Endpoints.Queries.SearchPeople;

public sealed class SearchPeopleQuery : PagedQuery<SearchPeopleResponse>
{
    public string? EmployeeCode { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public PersonStatus? Status { get; init; }
}

public sealed record SearchPeopleResponse : PageResponse<SearchPeopleItem>;

public sealed class SearchPeopleItem
{
    public int Id { get; init; }
    public string EmployeeCode { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public PersonStatus Status { get; init; }
}
