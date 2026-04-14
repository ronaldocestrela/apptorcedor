using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateAdminGame;

public sealed record CreateAdminGameCommand(AdminGameWriteDto Dto) : IRequest<GameCreateResult>;
