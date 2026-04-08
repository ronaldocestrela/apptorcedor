namespace SocioTorcedor.Modules.Identity.Application.Contracts;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAtUtc) CreateAccessToken(
        string userId,
        string email,
        Guid tenantId,
        IReadOnlyList<string> roles,
        IReadOnlyList<string> permissions);
}
