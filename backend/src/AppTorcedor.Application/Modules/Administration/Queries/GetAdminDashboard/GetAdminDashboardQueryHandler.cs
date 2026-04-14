using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminDashboard;

public sealed class GetAdminDashboardQueryHandler(IAdminDashboardReadPort dashboard)
    : IRequestHandler<GetAdminDashboardQuery, AdminDashboardDto>
{
    public Task<AdminDashboardDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken) =>
        dashboard.GetAsync(cancellationToken);
}
