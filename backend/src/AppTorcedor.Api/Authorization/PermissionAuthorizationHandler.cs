using AppTorcedor.Identity;
using Microsoft.AspNetCore.Authorization;

namespace AppTorcedor.Api.Authorization;

public sealed class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (context.User.Claims.Any(c => c.Type == AppClaimTypes.Permission && c.Value == requirement.Permission))
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
