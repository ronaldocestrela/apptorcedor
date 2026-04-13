using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetNewsFeed;

public sealed record GetNewsFeedQuery(string? Search, int Page, int PageSize) : IRequest<TorcedorNewsFeedPageDto>;
