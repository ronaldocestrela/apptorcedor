using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Plans;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdatePlan;

public sealed class UpdatePlanCommandHandler(IPlansAdministrationPort plans)
    : IRequestHandler<UpdatePlanCommand, UpdatePlanResult>
{
    public async Task<UpdatePlanResult> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
    {
        var err = PlanWriteValidator.ValidateAll(request.Dto);
        if (err is not null)
            return new UpdatePlanResult(false, err);

        PlanBillingCycles.TryNormalize(request.Dto.BillingCycle, out var canonical);
        var dto = request.Dto with { BillingCycle = canonical };

        var updated = await plans.UpdatePlanAsync(request.PlanId, dto, cancellationToken).ConfigureAwait(false);
        return updated ? new UpdatePlanResult(false, null) : new UpdatePlanResult(true, null);
    }
}
