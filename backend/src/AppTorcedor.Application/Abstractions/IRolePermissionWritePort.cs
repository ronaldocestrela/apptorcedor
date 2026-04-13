namespace AppTorcedor.Application.Abstractions;

public interface IRolePermissionWritePort
{
    /// <summary>Replaces all permissions linked to the role (by name). Permission names must exist in <c>AppPermissions</c>.</summary>
    Task ReplaceRolePermissionsAsync(string roleName, IReadOnlyList<string> permissionNames, CancellationToken cancellationToken = default);
}
