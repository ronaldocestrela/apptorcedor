namespace SocioTorcedor.Modules.Membership.Application.Contracts;

/// <summary>
/// Resolves the authenticated user id (JWT sub) for tenant routes.
/// </summary>
public interface ICurrentUserAccessor
{
    string? GetUserId();
}
