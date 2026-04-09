using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.RevokeTenantPlan;

public sealed class RevokeTenantPlanHandler(ITenantPlanRepository repository)
    : ICommandHandler<RevokeTenantPlanCommand>
{
    public async Task<Result> Handle(RevokeTenantPlanCommand command, CancellationToken cancellationToken)
    {
        var assignment = await repository.GetByIdAsync(command.TenantPlanId, cancellationToken);
        if (assignment is null)
            return Result.Fail(Error.NotFound("TenantPlan.NotFound", "Tenant plan assignment not found."));

        if (assignment.Status != TenantPlanStatus.Active)
            return Result.Fail(Error.Failure("TenantPlan.NotActive", "Only an active assignment can be revoked."));

        assignment.Revoke();
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
