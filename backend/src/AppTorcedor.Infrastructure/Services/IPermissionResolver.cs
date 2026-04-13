namespace AppTorcedor.Infrastructure.Services;

public interface IPermissionResolver
{
    Task<IReadOnlyList<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames, CancellationToken cancellationToken = default);
}
