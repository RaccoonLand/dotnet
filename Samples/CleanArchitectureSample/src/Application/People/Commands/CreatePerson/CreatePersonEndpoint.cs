using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.People.Domain.Entities;
using CleanArchitectureSample.People.Domain.ValueObjects;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.ValueObjects;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.FileStorage.Abstractions;

namespace CleanArchitectureSample.Application.People.Commands.CreatePerson;

public sealed class CreatePersonEndpoint(
    ICommandDbContext db,
    IFileStorage fileStorage)
    : IEndpoint<CreatePersonCommand, int>
{
    public async Task<Result<int>> ExecuteAsync(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var person = Person.Create(
            EmployeeCode.From(request.EmployeeCode),
            FirstName.From(request.FirstName),
            LastName.From(request.LastName),
            NationalCode.From(request.NationalCode),
            Email.From(request.Email),
            MobileNumber.From(request.MobileNumber),
            request.EmploymentDate);

        var metadata = PersonFileKeys.CreateMetadata(person.BusinessKey);

        var photoPut = await fileStorage.PutAsync(
            FileStoragePutHelper.CreateRequest(
                request.PhotoContent,
                request.PhotoContentType,
                FilePutConstraints.Images,
                PutMode.CreateOnly,
                key: PersonFileKeys.Photo(person.BusinessKey),
                contentLength: request.PhotoContentLength,
                metadata: metadata),
            cancellationToken);

        var resumePut = await fileStorage.PutAsync(
            FileStoragePutHelper.CreateRequest(
                request.ResumeContent,
                request.ResumeContentType,
                FilePutConstraints.PdfDocuments,
                PutMode.CreateOnly,
                key: PersonFileKeys.Resume(person.BusinessKey),
                contentLength: request.ResumeContentLength,
                metadata: metadata),
            cancellationToken);

        person.SetPhotoFileKey(photoPut.File.Key);
        person.SetResumeFileKey(resumePut.File.Key);

        db.Set<Person>().Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return Result<int>.Success(person.Id);
    }
}
