using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.ListTorcedorGames;

public sealed record ListTorcedorGamesQuery(string? Search, int Page, int PageSize) : IRequest<TorcedorGameListPageDto>;
