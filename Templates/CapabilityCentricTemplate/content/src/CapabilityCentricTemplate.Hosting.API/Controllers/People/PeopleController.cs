using CapabilityCentricTemplate.People.Endpoints.Commands.CreatePerson;
using CapabilityCentricTemplate.People.Endpoints.Queries.GetPersonById;
using CapabilityCentricTemplate.People.Endpoints.Queries.SearchPeople;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.Hosting.AspNetCore.Controllers;

namespace CapabilityCentricTemplate.Hosting.API.Controllers.People;

[ApiController]
[Route("api/[controller]")]
public sealed class PeopleController : RaccoonLandController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePersonCommand command, CancellationToken cancellationToken)
        => await DispatchAsync(command, cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        => await DispatchAsync(new GetPersonByIdQuery { Id = id }, cancellationToken);

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] SearchPeopleQuery query, CancellationToken cancellationToken)
        => await DispatchAsync(query, cancellationToken);
}
