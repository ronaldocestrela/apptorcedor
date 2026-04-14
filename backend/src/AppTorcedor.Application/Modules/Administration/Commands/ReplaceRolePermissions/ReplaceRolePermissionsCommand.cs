using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplaceRolePermissions;

public sealed record ReplaceRolePermissionsCommand(string RoleName, IReadOnlyList<string> PermissionNames) : IRequest;
