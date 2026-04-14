namespace AppTorcedor.Api.Contracts;

public sealed record ReplaceRolePermissionsRequest(string RoleName, IReadOnlyList<string> PermissionNames);
