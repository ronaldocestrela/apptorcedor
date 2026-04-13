using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetPlanDetails;

public sealed class GetPlanDetailsQueryHandler(ITorcedorPublishedPlansReadPort port)
    : IRequestHandler<GetPlanDetailsQuery, TorcedorPublishedPlanDetailDto?>
{
    public Task<TorcedorPublishedPlanDetailDto?> Handle(
        GetPlanDetailsQuery request,
        CancellationToken cancellationToken) =>
        port.GetPublishedActiveByIdAsync(request.PlanId, cancellationToken);
}
