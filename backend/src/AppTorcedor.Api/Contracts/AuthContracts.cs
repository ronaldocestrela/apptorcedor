namespace AppTorcedor.Api.Contracts;

public sealed record LoginRequest(string Email, string Password);

public sealed record RefreshRequest(string RefreshToken);

public sealed record LogoutRequest(string RefreshToken);

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    IReadOnlyList<string> Roles);

public sealed record MeResponse(
    Guid Id,
    string Email,
    string Name,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);
