using CleanArchitectureSample.Application.Abstractions.Persistence;
using CleanArchitectureSample.Departments.Shared;
using CleanArchitectureSample.Shared.Localizations;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using Department = CleanArchitectureSample.Departments.Domain.Entities.Department;

namespace CleanArchitectureSample.Application.Departments.Commands.DeleteDepartment;

public sealed class DeleteDepartmentEndpoint(
    ICommandDbContext db,
    IMessageLocalization messageLocalization)
    : IEndpoint<DeleteDepartmentCommand>
{
    public async Task<Result> ExecuteAsync(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await db.Set<Department>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (department is null)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization[SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, DepartmentLocalizations.DEPARTMENT]));
        }

        db.Set<Department>().Remove(department);
        await db.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
