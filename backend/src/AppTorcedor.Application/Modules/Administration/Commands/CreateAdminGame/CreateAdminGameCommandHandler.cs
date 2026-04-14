using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateAdminGame;

public sealed class CreateAdminGameCommandHandler(IGameAdministrationPort games)
    : IRequestHandler<CreateAdminGameCommand, GameCreateResult>
{
    public Task<GameCreateResult> Handle(CreateAdminGameCommand request, CancellationToken cancellationToken) =>
        games.CreateGameAsync(request.Dto, cancellationToken);
}
