using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.ToggleSaaSPlan;

public sealed class ToggleSaaSPlanHandler(ISaaSPlanRepository repository)
    : ICommandHandler<ToggleSaaSPlanCommand>
{
    public async Task<Result> Handle(ToggleSaaSPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await repository.GetTrackedByIdAsync(command.Id, cancellationToken);
        if (plan is null)
            return Result.Fail(Error.NotFound("SaaSPlan.NotFound", "SaaS plan not found."));

        plan.ToggleActive();
        await repository.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
