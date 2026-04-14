using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminPlans;

public sealed class ListAdminPlansQueryHandler(IPlansAdministrationPort plans)
    : IRequestHandler<ListAdminPlansQuery, AdminPlanListPageDto>
{
    public Task<AdminPlanListPageDto> Handle(ListAdminPlansQuery request, CancellationToken cancellationToken) =>
        plans.ListPlansAsync(
            request.Search,
            request.IsActive,
            request.IsPublished,
            request.Page,
            request.PageSize,
            cancellationToken);
}
