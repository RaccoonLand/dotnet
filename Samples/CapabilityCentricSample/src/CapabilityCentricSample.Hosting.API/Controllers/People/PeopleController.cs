using CapabilityCentricSample.People.Endpoints.Commands.CreatePerson;
using CapabilityCentricSample.People.Endpoints.Commands.DeletePerson;
using CapabilityCentricSample.People.Endpoints.Commands.UpdatePerson;
using CapabilityCentricSample.People.Endpoints.Processes.AssignPersonToDepartment;
using CapabilityCentricSample.People.Endpoints.Queries.GetPersonById;
using CapabilityCentricSample.People.Endpoints.Queries.SearchPeople;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.Hosting.AspNetCore.Controllers;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;

namespace CapabilityCentricSample.Hosting.API.Controllers.People;

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

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        [FromRoute] int id,
        [FromBody] UpdatePersonCommand command,
        CancellationToken cancellationToken)
        => await DispatchAsync(command with { Id = id }, cancellationToken);

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete([FromRoute] int id, CancellationToken cancellationToken)
        => await DispatchAsync(new DeletePersonCommand { Id = id }, cancellationToken);

    [HttpPost("{id:int}/assign-to-department")]
    public async Task<IActionResult> AssignToDepartment(
        [FromRoute] int id,
        [FromBody] AssignPersonToDepartmentCommand command,
        CancellationToken cancellationToken)
        => await DispatchAsync(command with { PersonId = id }, cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        => await DispatchAsync(new GetPersonByIdQuery { Id = id }, cancellationToken);

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] SearchPeopleQuery query, CancellationToken cancellationToken)
        => await DispatchAsync(query, cancellationToken);
}
