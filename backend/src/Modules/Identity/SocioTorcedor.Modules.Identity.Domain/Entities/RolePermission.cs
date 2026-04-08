using SocioTorcedor.BuildingBlocks.Domain.Abstractions;

namespace SocioTorcedor.Modules.Identity.Domain.Entities;

public sealed class RolePermission : Entity
{
    private RolePermission()
    {
    }

    public string RoleId { get; private set; } = null!;

    public Guid PermissionId { get; private set; }

    public Permission Permission { get; private set; } = null!;

    public static RolePermission Create(string roleId, Guid permissionId)
    {
        if (string.IsNullOrWhiteSpace(roleId))
            throw new ArgumentException("RoleId is required.", nameof(roleId));
        if (permissionId == Guid.Empty)
            throw new ArgumentException("PermissionId is required.", nameof(permissionId));

        return new RolePermission
        {
            RoleId = roleId.Trim(),
            PermissionId = permissionId
        };
    }
}
