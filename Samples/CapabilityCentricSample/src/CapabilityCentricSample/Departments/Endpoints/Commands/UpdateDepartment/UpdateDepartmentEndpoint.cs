using CapabilityCentricSample.Departments.Domain.ValueObjects;
using CapabilityCentricSample.Departments.Shared;
using CapabilityCentricSample.Shared.Localizations;
using CapabilityCentricSample.Shared.Persistence.Commands;
using CapabilityCentricSample.Shared.ValueObjects;
using Microsoft.EntityFrameworkCore;
using RaccoonLand.Core.RequestProcessing.Abstractions.Cqrs;
using RaccoonLand.Core.RequestProcessing.Abstractions.Responses;
using RaccoonLand.Core.RequestProcessing.Abstractions.Results;
using RaccoonLand.Modules.MessageLocalization.Abstraction;
using Department = CapabilityCentricSample.Departments.Domain.Entities.Department;

namespace CapabilityCentricSample.Departments.Endpoints.Commands.UpdateDepartment;

public sealed class UpdateDepartmentEndpoint(
    CapabilityCentricSampleCommandDbContext dbContext,
    IMessageLocalization messageLocalization)
    : IEndpoint<UpdateDepartmentCommand>
{
    public async Task<Result> ExecuteAsync(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await dbContext.Set<Department>()
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

        await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
