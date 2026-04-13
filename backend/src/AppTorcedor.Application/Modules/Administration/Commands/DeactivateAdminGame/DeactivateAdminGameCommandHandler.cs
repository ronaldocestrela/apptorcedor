using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.DeactivateAdminGame;

public sealed class DeactivateAdminGameCommandHandler(IGameAdministrationPort games)
    : IRequestHandler<DeactivateAdminGameCommand, GameMutationResult>
{
    public Task<GameMutationResult> Handle(DeactivateAdminGameCommand request, CancellationToken cancellationToken) =>
        games.DeactivateGameAsync(request.GameId, cancellationToken);
}
