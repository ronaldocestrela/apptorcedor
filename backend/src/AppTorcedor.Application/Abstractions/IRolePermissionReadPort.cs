namespace AppTorcedor.Application.Abstractions;

public interface IRolePermissionReadPort
{
    Task<IReadOnlyList<RolePermissionRowDto>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed record RolePermissionRowDto(string RoleName, string PermissionName);
