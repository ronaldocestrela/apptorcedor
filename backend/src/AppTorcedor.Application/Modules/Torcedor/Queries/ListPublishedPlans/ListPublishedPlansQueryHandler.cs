using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListPublishedPlans;

public sealed class ListPublishedPlansQueryHandler(ITorcedorPublishedPlansReadPort port)
    : IRequestHandler<ListPublishedPlansQuery, TorcedorPublishedPlansCatalogDto>
{
    public Task<TorcedorPublishedPlansCatalogDto> Handle(
        ListPublishedPlansQuery request,
        CancellationToken cancellationToken) =>
        port.ListPublishedActiveAsync(cancellationToken);
}
