using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListPublishedPlans;

public sealed record ListPublishedPlansQuery : IRequest<TorcedorPublishedPlansCatalogDto>;
