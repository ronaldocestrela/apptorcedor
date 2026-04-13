using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.ReplaceRolePermissions;

public sealed class ReplaceRolePermissionsCommandHandler(IRolePermissionWritePort writePort)
    : IRequestHandler<ReplaceRolePermissionsCommand>
{
    public async Task Handle(ReplaceRolePermissionsCommand request, CancellationToken cancellationToken)
    {
        if (request.RoleName == SystemRoles.AdministradorMaster && request.PermissionNames.Count == 0)
            throw new InvalidOperationException("Cannot remove all permissions from Administrador Master.");

        await writePort
            .ReplaceRolePermissionsAsync(request.RoleName, request.PermissionNames, cancellationToken)
            .ConfigureAwait(false);
    }
}
