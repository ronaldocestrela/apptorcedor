namespace SocioTorcedor.Modules.Identity.Application.DTOs;

public sealed record AuthResultDto(string AccessToken, DateTime ExpiresAtUtc);
