using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminNews;

public sealed class GetAdminNewsQueryHandler(INewsAdministrationPort news)
    : IRequestHandler<GetAdminNewsQuery, AdminNewsDetailDto?>
{
    public Task<AdminNewsDetailDto?> Handle(GetAdminNewsQuery request, CancellationToken cancellationToken) =>
        news.GetNewsByIdAsync(request.NewsId, cancellationToken);
}
