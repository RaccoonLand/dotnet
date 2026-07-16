using CapabilityCentricSample.People.Persistence.Queries;
using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Queries;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.FileStorage.Abstractions;
using RaccoonLand.Modules.MessageLocalization.Abstraction;

namespace CapabilityCentricSample.People.Endpoints.Queries.DownloadPersonResume;

public sealed class DownloadPersonResumeEndpoint(
    CapabilityCentricSampleQueryDbContext dbContext,
    IFileStorage fileStorage,
    IMessageLocalization messageLocalization)
    : IEndpoint<DownloadPersonResumeQuery, DownloadPersonFileResult?>
{
    public async Task<Result<DownloadPersonFileResult?>> ExecuteAsync(
        DownloadPersonResumeQuery request,
        CancellationToken cancellationToken)
    {
        var person = await dbContext.Set<Person>()
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (person is null)
            return Result<DownloadPersonFileResult?>.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON]));

        if (string.IsNullOrWhiteSpace(person.ResumeFileKey))
            return Result<DownloadPersonFileResult?>.Success(null);

        var open = await fileStorage.OpenReadAsync(
            new OpenReadRequest { Key = person.ResumeFileKey },
            cancellationToken);

        return Result<DownloadPersonFileResult?>.Success(new DownloadPersonFileResult
        {
            Content = open.Content,
            ContentType = open.File.ContentType ?? "application/pdf",
            FileName = "resume.pdf",
            ContentLength = open.File.Length,
        });
    }
}
