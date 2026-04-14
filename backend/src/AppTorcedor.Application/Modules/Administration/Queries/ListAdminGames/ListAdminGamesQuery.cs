using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminGames;

public sealed record ListAdminGamesQuery(string? Search, bool? IsActive, int Page, int PageSize)
    : IRequest<AdminGameListPageDto>;
