using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetPlanDetails;

public sealed record GetPlanDetailsQuery(Guid PlanId) : IRequest<TorcedorPublishedPlanDetailDto?>;
