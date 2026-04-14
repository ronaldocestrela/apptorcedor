using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminGames;

public sealed class ListAdminGamesQueryHandler(IGameAdministrationPort games)
    : IRequestHandler<ListAdminGamesQuery, AdminGameListPageDto>
{
    public Task<AdminGameListPageDto> Handle(ListAdminGamesQuery request, CancellationToken cancellationToken) =>
        games.ListGamesAsync(request.Search, request.IsActive, request.Page, request.PageSize, cancellationToken);
}
