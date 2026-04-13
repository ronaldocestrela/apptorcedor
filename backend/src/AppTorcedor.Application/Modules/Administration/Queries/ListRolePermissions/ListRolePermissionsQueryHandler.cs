using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListRolePermissions;

public sealed class ListRolePermissionsQueryHandler(IRolePermissionReadPort readModel)
    : IRequestHandler<ListRolePermissionsQuery, IReadOnlyList<RolePermissionRowDto>>
{
    public Task<IReadOnlyList<RolePermissionRowDto>> Handle(ListRolePermissionsQuery request, CancellationToken cancellationToken) =>
        readModel.ListAsync(cancellationToken);
}
