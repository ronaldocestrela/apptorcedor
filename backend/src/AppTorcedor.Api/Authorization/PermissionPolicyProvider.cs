using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Api.Authorization;

public sealed class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Policies.PermissionPrefix, StringComparison.Ordinal))
        {
            var permission = policyName[Policies.PermissionPrefix.Length..];
            var policy = new AuthorizationPolicyBuilder().AddRequirements(new PermissionRequirement(permission)).Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();
}
