using CapabilityCentricSample.Departments.Endpoints.Commands.CreateDepartment;
using CapabilityCentricSample.Departments.Endpoints.Commands.DeleteDepartment;
using CapabilityCentricSample.Departments.Endpoints.Commands.UpdateDepartment;
using CapabilityCentricSample.Departments.Endpoints.Queries.GetDepartmentById;
using CapabilityCentricSample.Departments.Endpoints.Queries.SearchDepartments;
using Microsoft.AspNetCore.Mvc;
using RaccoonLand.Core.Hosting.AspNetCore.Controllers;

namespace CapabilityCentricSample.Hosting.API.Controllers.Departments;

[ApiController]
[Route("api/[controller]")]
public sealed class DepartmentsController : RaccoonLandController
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentCommand command, CancellationToken cancellationToken)
        => await DispatchAsync(command, cancellationToken);

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateDepartmentCommand command,
        CancellationToken cancellationToken)
        => await DispatchAsync(command with { Id = id }, cancellationToken);

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
        => await DispatchAsync(new DeleteDepartmentCommand { Id = id }, cancellationToken);

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
        => await DispatchAsync(new GetDepartmentByIdQuery { Id = id }, cancellationToken);

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] SearchDepartmentsQuery query, CancellationToken cancellationToken)
        => await DispatchAsync(query, cancellationToken);
}
