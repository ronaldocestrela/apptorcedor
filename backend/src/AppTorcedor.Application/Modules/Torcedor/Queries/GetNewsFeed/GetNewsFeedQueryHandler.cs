using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetNewsFeed;

public sealed class GetNewsFeedQueryHandler(ITorcedorNewsReadPort port)
    : IRequestHandler<GetNewsFeedQuery, TorcedorNewsFeedPageDto>
{
    public Task<TorcedorNewsFeedPageDto> Handle(GetNewsFeedQuery request, CancellationToken cancellationToken) =>
        port.ListPublishedAsync(request.Search, request.Page, request.PageSize, cancellationToken);
}
