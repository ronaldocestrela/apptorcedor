using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminNews;

public sealed record ListAdminNewsQuery(
    string? Search,
    NewsEditorialStatus? Status,
    int Page,
    int PageSize) : IRequest<AdminNewsListPageDto>;
