using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminGame;

public sealed class UpdateAdminGameCommandHandler(IGameAdministrationPort games)
    : IRequestHandler<UpdateAdminGameCommand, GameMutationResult>
{
    public Task<GameMutationResult> Handle(UpdateAdminGameCommand request, CancellationToken cancellationToken) =>
        games.UpdateGameAsync(request.GameId, request.Dto, cancellationToken);
}
