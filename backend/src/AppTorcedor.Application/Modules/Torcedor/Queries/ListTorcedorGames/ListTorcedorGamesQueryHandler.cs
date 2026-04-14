using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListTorcedorGames;

public sealed class ListTorcedorGamesQueryHandler(IGameTorcedorReadPort games)
    : IRequestHandler<ListTorcedorGamesQuery, TorcedorGameListPageDto>
{
    public Task<TorcedorGameListPageDto> Handle(ListTorcedorGamesQuery request, CancellationToken cancellationToken) =>
        games.ListActiveGamesAsync(request.Search, request.Page, request.PageSize, cancellationToken);
}
