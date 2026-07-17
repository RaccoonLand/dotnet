using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Commands;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using Department = CapabilityCentricSample.Departments.Domain.Entities.Department;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.DeleteDepartment;

public sealed class DeleteDepartmentEndpoint(
    CapabilityCentricSampleCommandDbContext dbContext,
    IMessageLocalization messageLocalization)
    : IEndpoint<DeleteDepartmentCommand>
{
    public async Task<Result> ExecuteAsync(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await dbContext.Set<Department>()
            .SingleOrDefaultAsync(x => x.Id == request.Id, cancellationToken);

        if (department is null)
        {
            return Result.Failure(new PipelineMessage(
                SharedBusinessMessageTemplates.ENTITY_NOT_FOUND,
                messageLocalization.Get(SharedBusinessMessageTemplates.ENTITY_NOT_FOUND, DepartmentLocalizations.DEPARTMENT)));
        }

        dbContext.Remove(department);
        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
