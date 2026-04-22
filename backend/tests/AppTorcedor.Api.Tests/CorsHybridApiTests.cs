using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppTorcedor.Application.Modules.Administration;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class CorsHybridApiTests(CorsHybridWebApplicationFactory factory) : IClassFixture<CorsHybridWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Preflight_allows_configured_static_origin()
    {
        using var req = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        req.Headers.TryAddWithoutValidation("Origin", "https://static-cors.example");
        req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.True(res.Headers.TryGetValues("Access-Control-Allow-Origin", out var allow));
        Assert.Contains("https://static-cors.example", allow);
    }

    [Fact]
    public async Task Preflight_blocks_unknown_origin_when_allowlist_nonempty()
    {
        using var req = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        req.Headers.TryAddWithoutValidation("Origin", "https://evil.example");
        req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");

        var res = await _client.SendAsync(req);
        Assert.False(res.Headers.Contains("Access-Control-Allow-Origin"));
    }

    [Fact]
    public async Task Preflight_allows_dynamic_origin_after_admin_upsert()
    {
        var token = await LoginAdminAsync();
        using var put = new HttpRequestMessage(
            HttpMethod.Put,
            "/api/admin/config/" + Uri.EscapeDataString(CorsConfigurationKeys.AllowedOriginsExtra));
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(new { value = """["https://dynamic-cors.example"]""" });
        var upsert = await _client.SendAsync(put);
        upsert.EnsureSuccessStatusCode();

        using var req = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        req.Headers.TryAddWithoutValidation("Origin", "https://dynamic-cors.example");
        req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");

        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.True(res.Headers.TryGetValues("Access-Control-Allow-Origin", out var allow));
        Assert.Contains("https://dynamic-cors.example", allow);
    }

    [Fact]
    public async Task Static_origin_still_allowed_after_dynamic_upsert()
    {
        var token = await LoginAdminAsync();
        using var put = new HttpRequestMessage(
            HttpMethod.Put,
            "/api/admin/config/" + Uri.EscapeDataString(CorsConfigurationKeys.AllowedOriginsExtra));
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(new { value = """["https://other.example"]""" });
        await _client.SendAsync(put);

        using var req = new HttpRequestMessage(HttpMethod.Options, "/health/live");
        req.Headers.TryAddWithoutValidation("Origin", "https://static-cors.example");
        req.Headers.TryAddWithoutValidation("Access-Control-Request-Method", "GET");

        var res = await _client.SendAsync(req);
        Assert.True(res.Headers.TryGetValues("Access-Control-Allow-Origin", out var allow));
        Assert.Contains("https://static-cors.example", allow);
    }

    private async Task<string> LoginAdminAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.False(string.IsNullOrEmpty(tokens?.AccessToken));
        return tokens!.AccessToken;
    }

    private sealed record AuthTokens(string AccessToken);
}
