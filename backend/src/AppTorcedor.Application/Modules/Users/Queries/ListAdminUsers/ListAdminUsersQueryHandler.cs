using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Queries.ListAdminUsers;

public sealed class ListAdminUsersQueryHandler(IUserAdministrationPort users)
    : IRequestHandler<ListAdminUsersQuery, AdminUserListPageDto>
{
    public Task<AdminUserListPageDto> Handle(ListAdminUsersQuery request, CancellationToken cancellationToken) =>
        users.ListUsersAsync(request.Search, request.IsActive, request.Page, request.PageSize, cancellationToken);
}
