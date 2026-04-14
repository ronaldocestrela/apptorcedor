using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminGame;

public sealed class GetAdminGameQueryHandler(IGameAdministrationPort games)
    : IRequestHandler<GetAdminGameQuery, AdminGameDetailDto?>
{
    public Task<AdminGameDetailDto?> Handle(GetAdminGameQuery request, CancellationToken cancellationToken) =>
        games.GetGameByIdAsync(request.GameId, cancellationToken);
}
