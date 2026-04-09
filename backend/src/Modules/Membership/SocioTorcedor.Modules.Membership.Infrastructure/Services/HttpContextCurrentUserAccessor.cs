using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SocioTorcedor.Modules.Membership.Application.Contracts;

namespace SocioTorcedor.Modules.Membership.Infrastructure.Services;

public sealed class HttpContextCurrentUserAccessor(IHttpContextAccessor httpContextAccessor) : ICurrentUserAccessor
{
    public string? GetUserId() =>
        httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
