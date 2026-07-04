using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Departments.Shared.Enums;
using CapabilityCentricSample.People.Domain.Entities;
using CapabilityCentricSample.People.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Commands;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using Department = CapabilityCentricSample.Departments.Domain.Entities.Department;

namespace CapabilityCentricSample.People.Endpoints.Processes.AssignPersonToDepartment;

public sealed class AssignPersonToDepartmentEndpoint(
    CapabilityCentricSampleCommandDbContext dbContext,
    IMessageLocalization messageLocalization)
    : IEndpoint<AssignPersonToDepartmentCommand>
{
    public async Task<Result> ExecuteAsync(AssignPersonToDepartmentCommand request, CancellationToken cancellationToken)
    {
        var person = await dbContext.Set<Person>()
            .SingleOrDefaultAsync(x => x.Id == request.PersonId, cancellationToken);

        if (person is null)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, PersonLocalizations.PERSON]));
        }

        var department = await dbContext.Set<Department>()
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
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
