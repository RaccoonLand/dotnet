using CapabilityCentricSample.People.Endpoints.Commands.CreatePerson;
using CapabilityCentricSample.People.Endpoints.Commands.DeletePerson;
using CapabilityCentricSample.People.Endpoints.Commands.SetPersonPhoto;
using CapabilityCentricSample.People.Endpoints.Commands.SetPersonResume;
using CapabilityCentricSample.People.Endpoints.Commands.UpdatePerson;
using CapabilityCentricSample.People.Endpoints.Processes.AssignPersonToDepartment;
using CapabilityCentricSample.People.Endpoints.Queries;
using CapabilityCentricSample.People.Endpoints.Queries.DownloadPersonPhoto;
using CapabilityCentricSample.People.Endpoints.Queries.DownloadPersonResume;
using CapabilityCentricSample.People.Endpoints.Queries.GetPersonById;
using CapabilityCentricSample.People.Endpoints.Queries.SearchPeople;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using RaccoonLand.Core.Hosting.AspNetCore.Controllers;
using RaccoonLand.Core.Hosting.AspNetCore.PipelineResponseMapping;
using RaccoonLand.Core.RequestProcessing.Abstractions.Dispatch;
using RaccoonLand.Modules.FileStorage.AspNetCore;

namespace CapabilityCentricSample.Hosting.API.Controllers.People;

[ApiController]
[Route("api/[controller]")]
public sealed class PeopleController : RaccoonLandController
{
    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(30 * 1024 * 1024)]
    public async Task<IActionResult> Create(
        [FromForm] CreatePersonCommand command,
        IFormFile photo,
        IFormFile resume,
        CancellationToken cancellationToken)
    {
        await using var photoUpload = photo.ToFileUploadContent();
        await using var resumeUpload = resume.ToFileUploadContent();

        return await DispatchAsync(
            new CreatePersonCommand
            {
                EmployeeCode = command.EmployeeCode,
                FirstName = command.FirstName,
                LastName = command.LastName,
                NationalCode = command.NationalCode,
                Email = command.Email,
                MobileNumber = command.MobileNumber,
                EmploymentDate = command.EmploymentDate,
                PhotoContent = photoUpload.Content,
                PhotoContentType = photoUpload.ContentType,
                PhotoContentLength = photoUpload.ContentLength,
                ResumeContent = resumeUpload.Content,
                ResumeContentType = resumeUpload.ContentType,
                ResumeContentLength = resumeUpload.ContentLength,
            },
            cancellationToken);
    }

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

    [HttpPost("{id:int}/photo")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> SetPhoto(
        [FromRoute] int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var upload = file.ToFileUploadContent();
        return await DispatchAsync(
            new SetPersonPhotoCommand
            {
                Id = id,
                Content = upload.Content,
                ContentType = upload.ContentType,
                ContentLength = upload.ContentLength,
            },
            cancellationToken);
    }

    [HttpPost("{id:int}/resume")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    public async Task<IActionResult> SetResume(
        [FromRoute] int id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        await using var upload = file.ToFileUploadContent();
        return await DispatchAsync(
            new SetPersonResumeCommand
            {
                Id = id,
                Content = upload.Content,
                ContentType = upload.ContentType,
                ContentLength = upload.ContentLength,
            },
            cancellationToken);
    }

    [HttpGet("{id:int}/photo")]
    public Task<IActionResult> DownloadPhoto([FromRoute] int id, CancellationToken cancellationToken)
        => DispatchFileAsync(new DownloadPersonPhotoQuery { Id = id }, cancellationToken);

    [HttpGet("{id:int}/resume")]
    public Task<IActionResult> DownloadResume([FromRoute] int id, CancellationToken cancellationToken)
        => DispatchFileAsync(new DownloadPersonResumeQuery { Id = id }, cancellationToken);

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById([FromRoute] int id, CancellationToken cancellationToken)
        => await DispatchAsync(new GetPersonByIdQuery { Id = id }, cancellationToken);

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] SearchPeopleQuery query, CancellationToken cancellationToken)
        => await DispatchAsync(query, cancellationToken);

    private async Task<IActionResult> DispatchFileAsync(
        RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs.IRequest<DownloadPersonFileResult?> request,
        CancellationToken cancellationToken)
    {
        var dispatcher = HttpContext.RequestServices.GetRequiredService<IRequestDispatcher>();
        var responseMapper = HttpContext.RequestServices.GetRequiredService<IPipelineResponseMapper>();

        var response = await dispatcher.DispatchAsync(
            request,
            HttpContext.RequestServices,
            cancellationToken);

        if (response?.Result is DownloadPersonFileResult file)
        {
            return File(
                file.Content,
                file.ContentType ?? "application/octet-stream",
                file.FileName);
        }

        if (response is { Errors.Count: 0 })
            return NotFound();

        return responseMapper.Map(response);
    }
}
