using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreatePlan;

public sealed record CreatePlanCommand(AdminPlanWriteDto Dto) : IRequest<CreatePlanResult>;

public sealed record CreatePlanResult(Guid? PlanId, string? ErrorMessage)
{
    public bool Ok => PlanId is not null && ErrorMessage is null;
}
