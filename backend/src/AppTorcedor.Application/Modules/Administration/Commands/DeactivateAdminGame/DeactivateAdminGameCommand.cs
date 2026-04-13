using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.DeactivateAdminGame;

public sealed record DeactivateAdminGameCommand(Guid GameId) : IRequest<GameMutationResult>;
