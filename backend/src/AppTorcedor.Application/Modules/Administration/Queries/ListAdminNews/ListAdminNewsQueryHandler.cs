using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminNews;

public sealed class ListAdminNewsQueryHandler(INewsAdministrationPort news)
    : IRequestHandler<ListAdminNewsQuery, AdminNewsListPageDto>
{
    public Task<AdminNewsListPageDto> Handle(ListAdminNewsQuery request, CancellationToken cancellationToken) =>
        news.ListNewsAsync(request.Search, request.Status, request.Page, request.PageSize, cancellationToken);
}
