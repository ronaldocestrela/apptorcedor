using AppTorcedor.Identity;

namespace AppTorcedor.Api.Services;

public interface IJwtTokenIssuer
{
    (string AccessToken, int ExpiresInSeconds) IssueAccessToken(ApplicationUser user, IList<string> roles);
}
