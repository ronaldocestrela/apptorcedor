using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminGame;

public sealed record GetAdminGameQuery(Guid GameId) : IRequest<AdminGameDetailDto?>;
