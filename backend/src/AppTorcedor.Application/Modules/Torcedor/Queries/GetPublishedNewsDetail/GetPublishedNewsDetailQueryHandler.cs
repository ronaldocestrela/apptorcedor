using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetPublishedNewsDetail;

public sealed class GetPublishedNewsDetailQueryHandler(ITorcedorNewsReadPort port)
    : IRequestHandler<GetPublishedNewsDetailQuery, TorcedorNewsDetailDto?>
{
    public Task<TorcedorNewsDetailDto?> Handle(GetPublishedNewsDetailQuery request, CancellationToken cancellationToken) =>
        port.GetPublishedByIdAsync(request.NewsId, cancellationToken);
}
