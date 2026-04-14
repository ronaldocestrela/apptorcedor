using Microsoft.AspNetCore.Authorization;

namespace AppTorcedor.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
