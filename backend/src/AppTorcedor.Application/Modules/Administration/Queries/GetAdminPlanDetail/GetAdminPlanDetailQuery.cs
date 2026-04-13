using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminPlanDetail;

public sealed record GetAdminPlanDetailQuery(Guid PlanId) : IRequest<AdminPlanDetailDto?>;
