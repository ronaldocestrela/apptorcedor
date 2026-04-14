using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdatePlan;

public sealed record UpdatePlanCommand(Guid PlanId, AdminPlanWriteDto Dto) : IRequest<UpdatePlanResult>;

public sealed record UpdatePlanResult(bool NotFound, string? ValidationError)
{
    public bool Ok => !NotFound && ValidationError is null;
}
