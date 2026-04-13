using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminGame;

public sealed record UpdateAdminGameCommand(Guid GameId, AdminGameWriteDto Dto) : IRequest<GameMutationResult>;
