using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartAGovernanceAndObservabilityTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Diagnostics_forbidden_for_torcedor_without_permissions()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(tokens);

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/diagnostics/admin-master-only");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Health_live_returns_ok()
    {
        var res = await _client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Health_ready_returns_ok_when_database_is_up()
    {
        var res = await _client.GetAsync("/health/ready");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Admin_can_list_role_permissions()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/role-permissions");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var rows = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, rows.ValueKind);
        Assert.NotEmpty(rows.EnumerateArray());
    }

    [Fact]
    public async Task Membership_status_update_writes_audit_log()
    {
        var token = await LoginAdminAsync();
        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status");
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        patch.Content = JsonContent.Create(new { status = "Ativo", reason = "Teste de auditoria" });
        var updated = await _client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        using var auditReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/audit-logs?entityType=MembershipRecord&take=20");
        auditReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var auditRes = await _client.SendAsync(auditReq);
        Assert.Equal(HttpStatusCode.OK, auditRes.StatusCode);
        var logs = await auditRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, logs.ValueKind);
        Assert.Contains(
            logs.EnumerateArray(),
            e => e.TryGetProperty("entityType", out var et) && et.GetString() == "MembershipRecord");
    }

    [Fact]
    public async Task Configuration_upsert_is_readable_and_audited()
    {
        var token = await LoginAdminAsync();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/admin/config/feature.flags");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(new { value = """{"x":1}""" });
        var upsert = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.OK, upsert.StatusCode);

        using var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/config");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listRes = await _client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);

        using var auditReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/audit-logs?entityType=AppConfigurationEntry&take=20");
        auditReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var auditRes = await _client.SendAsync(auditReq);
        Assert.Equal(HttpStatusCode.OK, auditRes.StatusCode);
        var logs = await auditRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            logs.EnumerateArray(),
            e =>
                e.TryGetProperty("action", out var a)
                && (a.GetString() == "AppConfigurationEntry.Update" || a.GetString() == "AppConfigurationEntry.Create"));
    }

    [Fact]
    public async Task Welcome_email_template_keys_can_be_upserted_and_listed()
    {
        var token = await LoginAdminAsync();
        foreach (var (key, value) in new (string Key, string Value)[]
                 {
                     ("Email.Welcome.Subject", "Olá {{Name}}"),
                     ("Email.Welcome.Html", """<p>Teste {{BannerImage}}{{Name}}</p>"""),
                     ("Email.Welcome.ImageUrl", "https://cdn.example/welcome.png"),
                 })
        {
            using var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/config/{Uri.EscapeDataString(key)}");
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            put.Content = JsonContent.Create(new { value });
            var upsert = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.OK, upsert.StatusCode);
        }

        using var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/config");
        listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var listRes = await _client.SendAsync(listReq);
        Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
        var rows = await listRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, rows.ValueKind);
        var byKey = rows.EnumerateArray().ToDictionary(
            e => e.GetProperty("key").GetString()!,
            e => e.GetProperty("value").GetString());
        Assert.Equal("Olá {{Name}}", byKey["Email.Welcome.Subject"]);
        Assert.Equal("""<p>Teste {{BannerImage}}{{Name}}</p>""", byKey["Email.Welcome.Html"]);
        Assert.Equal("https://cdn.example/welcome.png", byKey["Email.Welcome.ImageUrl"]);
    }

    private async Task<string> LoginAdminAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(tokens?.AccessToken));
        return tokens!.AccessToken;
    }

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
