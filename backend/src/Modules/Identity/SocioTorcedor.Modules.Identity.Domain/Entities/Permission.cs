using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Domain.Entities;

public sealed class Permission : Entity
{
    private Permission()
    {
    }

    public string Name { get; private set; } = null!;

    public string Description { get; private set; } = null!;

    public PermissionType Type { get; private set; }

    public ICollection<RolePermission> RolePermissions { get; } = new List<RolePermission>();

    public static Permission Create(string name, string description, PermissionType type = PermissionType.Module)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Permission name is required.", nameof(name));

        var trimmed = name.Trim();
        if (trimmed.Contains(' ', StringComparison.Ordinal))
            throw new ArgumentException("Permission name cannot contain spaces.", nameof(name));

        return new Permission
        {
            Name = trimmed,
            Description = description?.Trim() ?? string.Empty,
            Type = type
        };
    }
}
