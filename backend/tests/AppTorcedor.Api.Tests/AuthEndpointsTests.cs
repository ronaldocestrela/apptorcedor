using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppTorcedor.Identity;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class AuthEndpointsTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_returns_tokens_for_seeded_admin()
    {
        var res = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(body?.AccessToken));
        Assert.False(string.IsNullOrEmpty(body.RefreshToken));
        Assert.Contains(SystemRoles.AdministradorMaster, body.Roles);
    }

    [Fact]
    public async Task Accept_staff_invite_with_invalid_token_returns_unauthorized()
    {
        var res = await _client.PostAsJsonAsync(
            "/api/auth/accept-staff-invite",
            new { token = "invalid-token", password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Login_with_bad_password_returns_unauthorized()
    {
        var res = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "WrongPassword1!" });
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Me_returns_user_when_authenticated()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(tokens);

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var me = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
        var profile = await me.Content.ReadFromJsonAsync<MeResponseDto>();
        Assert.Equal("admin@test.local", profile?.Email);
        Assert.Contains(SystemRoles.AdministradorMaster, profile?.Roles ?? []);
        Assert.Contains(ApplicationPermissions.AdministracaoDiagnostics, profile?.Permissions ?? []);
        Assert.Contains(ApplicationPermissions.ConfiguracoesVisualizar, profile?.Permissions ?? []);
    }

    [Fact]
    public async Task Admin_master_diagnostics_requires_role()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(tokens);

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/diagnostics/admin-master-only");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Refresh_rotates_refresh_token()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        var first = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(first);

        var refresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = first!.RefreshToken });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        var second = await refresh.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(second);
        Assert.NotEqual(first.RefreshToken, second!.RefreshToken);
        Assert.False(string.IsNullOrEmpty(second.AccessToken));
    }

    [Fact]
    public async Task Logout_revokes_refresh_token()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(tokens);

        var logout = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            new { refreshToken = tokens!.RefreshToken });
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        var refresh = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            new { refreshToken = tokens.RefreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
    }

    private sealed record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        IReadOnlyList<string> Roles);

    private sealed record MeResponseDto(
        Guid Id,
        string Email,
        string Name,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions);
}
