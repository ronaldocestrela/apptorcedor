namespace AppTorcedor.Infrastructure.Entities;

public sealed class AppRolePermission
{
    public Guid RoleId { get; set; }
    public Guid PermissionId { get; set; }
}
