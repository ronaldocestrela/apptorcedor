using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAdminPlans;

public sealed record ListAdminPlansQuery(
    string? Search,
    bool? IsActive,
    bool? IsPublished,
    int Page,
    int PageSize) : IRequest<AdminPlanListPageDto>;
