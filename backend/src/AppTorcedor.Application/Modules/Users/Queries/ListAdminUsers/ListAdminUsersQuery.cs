using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Queries.ListAdminUsers;

public sealed record ListAdminUsersQuery(string? Search, bool? IsActive, int Page = 1, int PageSize = 20)
    : IRequest<AdminUserListPageDto>;
