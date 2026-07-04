using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Departments.Domain.ValueObjects;
using CleanArchitectureSample.Departments.Shared;
using CleanArchitectureSample.Shared.Localizations;
using CleanArchitectureSample.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using Department = CleanArchitectureSample.Departments.Domain.Entities.Department;

namespace CleanArchitectureSample.Application.Departments.Commands.UpdateDepartment;

public sealed class UpdateDepartmentEndpoint(
    ICommandDbContext db,
    IMessageLocalization messageLocalization)
    : IEndpoint<UpdateDepartmentCommand>
{
    public async Task<Result> ExecuteAsync(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await db.Set<Department>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (department is null)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, DepartmentLocalizations.DEPARTMENT]));
        }

        department.Update(
            DepartmentName.From(request.Name),
            string.IsNullOrWhiteSpace(request.Description) ? null : Description.From(request.Description),
            request.Status);

        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
