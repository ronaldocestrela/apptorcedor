using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;

namespace SocioTorcedor.Modules.Backoffice.Application.Commands.UpdateSaaSPlan;

public sealed class UpdateSaaSPlanHandler(ISaaSPlanRepository repository)
    : ICommandHandler<UpdateSaaSPlanCommand>
{
    public async Task<Result> Handle(UpdateSaaSPlanCommand command, CancellationToken cancellationToken)
    {
        var plan = await repository.GetTrackedByIdAsync(command.Id, cancellationToken);
        if (plan is null)
            return Result.Fail(Error.NotFound("SaaSPlan.NotFound", "SaaS plan not found."));

        try
        {
            plan.Update(
                command.Name,
                command.Description,
                command.MonthlyPrice,
                command.YearlyPrice,
                command.MaxMembers,
                command.StripePriceMonthlyId,
                command.StripePriceYearlyId);

            if (command.Features is not null)
            {
                var features = command.Features
                    .Select(f => (f.Key, f.Description, f.Value))
                    .ToList();
                plan.ReplaceFeatures(features);
            }

            await repository.SaveChangesAsync(cancellationToken);
            return Result.Ok();
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(Error.Validation("SaaSPlan.Invalid", ex.Message));
        }
    }
}
