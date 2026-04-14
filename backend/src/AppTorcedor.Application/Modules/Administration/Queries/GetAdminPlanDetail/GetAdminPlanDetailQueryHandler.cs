using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminPlanDetail;

public sealed class GetAdminPlanDetailQueryHandler(IPlansAdministrationPort plans)
    : IRequestHandler<GetAdminPlanDetailQuery, AdminPlanDetailDto?>
{
    public Task<AdminPlanDetailDto?> Handle(GetAdminPlanDetailQuery request, CancellationToken cancellationToken) =>
        plans.GetPlanByIdAsync(request.PlanId, cancellationToken);
}
