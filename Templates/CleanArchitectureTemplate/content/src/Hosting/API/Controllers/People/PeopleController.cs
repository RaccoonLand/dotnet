using CleanArchitectureTemplate.Application.People.Commands.CreatePerson;
using CleanArchitectureTemplate.Application.People.Queries.GetPersonById;
using CleanArchitectureTemplate.Application.People.Queries.SearchPeople;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.Hosting.AspNetCore.Controllers;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;

namespace CleanArchitectureTemplate.Hosting.API.Controllers.People;

[ApiController]
[Route("api/[controller]")]
public sealed class PeopleController(
    IRequestDispatcher dispatcher,
    IPipelineResponseMapper responseMapper)
    : RaccoonLandController(dispatcher, responseMapper)
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