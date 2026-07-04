using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Departments.Shared;
using CleanArchitectureSample.Departments.Shared.Enums;
using CleanArchitectureSample.People.Domain.Entities;
using CleanArchitectureSample.People.Shared;
using CleanArchitectureSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using Department = CleanArchitectureSample.Departments.Domain.Entities.Department;

namespace CleanArchitectureSample.Application.People.Processes.AssignPersonToDepartment;

public sealed class AssignPersonToDepartmentEndpoint(
    ICommandDbContext db,
    IMessageLocalization messageLocalization)
    : IEndpoint<AssignPersonToDepartmentCommand>
{
    public async Task<Result> ExecuteAsync(AssignPersonToDepartmentCommand request, CancellationToken cancellationToken)
    {
        var person = await db.Set<Person>()
            .SingleOrDefaultAsync(x => x.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON]));
        }

        var department = await db.Set<Department>()
            .SingleOrDefaultAsync(x => x.Id == request.DepartmentId, cancellationToken);

        if (department is null)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, DepartmentLocalizations.DEPARTMENT]));
        }

        if (department.Status != DepartmentStatus.Active)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_IS_NOT_ACTIVE,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_IS_NOT_ACTIVE, DepartmentLocalizations.DEPARTMENT]));
        }

        person.AssignToDepartment(department.Id);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
