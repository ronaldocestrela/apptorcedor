using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Plans;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreatePlan;

public sealed class CreatePlanCommandHandler(IPlansAdministrationPort plans)
    : IRequestHandler<CreatePlanCommand, CreatePlanResult>
{
    public async Task<CreatePlanResult> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
    {
        var err = PlanWriteValidator.ValidateAll(request.Dto);
        if (err is not null)
            return new CreatePlanResult(null, err);

        PlanBillingCycles.TryNormalize(request.Dto.BillingCycle, out var canonical);
        var dto = request.Dto with { BillingCycle = canonical };

        var id = await plans.CreatePlanAsync(dto, cancellationToken).ConfigureAwait(false);
        return new CreatePlanResult(id, null);
    }
}
