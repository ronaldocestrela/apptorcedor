using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListRolePermissions;

public sealed record ListRolePermissionsQuery : IRequest<IReadOnlyList<RolePermissionRowDto>>;
