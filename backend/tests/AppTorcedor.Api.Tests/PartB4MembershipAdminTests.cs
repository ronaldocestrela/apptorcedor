using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB4MembershipAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_memberships_requires_socios_gerenciar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/memberships");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task List_memberships_returns_sample_row_for_admin()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/memberships?pageSize=50");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        Assert.Contains(
            items.EnumerateArray(),
            e =>
                e.TryGetProperty("membershipId", out var mid)
                && mid.GetString() == TestingSeedConstants.SampleMembershipId.ToString());
    }

    [Fact]
    public async Task Get_membership_detail_returns_ok()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(TestingSeedConstants.SampleMemberUserId.ToString(), body.GetProperty("userId").GetString());
    }

    [Fact]
    public async Task Patch_status_without_reason_returns_bad_request()
    {
        var token = await LoginAdminAsync();
        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status");
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        patch.Content = JsonContent.Create(new { status = "Ativo" });
        var res = await _client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Patch_status_with_reason_writes_domain_history()
    {
        var token = await LoginAdminAsync();
        var target = await PickDifferentStatusAsync(token);
        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status");
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        patch.Content = JsonContent.Create(new { status = target, reason = "Regularização manual" });
        var updated = await _client.SendAsync(patch);
        Assert.Equal(HttpStatusCode.NoContent, updated.StatusCode);

        using var histReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/history?take=10");
        histReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var histRes = await _client.SendAsync(histReq);
        Assert.Equal(HttpStatusCode.OK, histRes.StatusCode);
        var logs = await histRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, logs.ValueKind);
        Assert.Contains(
            logs.EnumerateArray(),
            e =>
                e.TryGetProperty("reason", out var r)
                && r.GetString() == "Regularização manual"
                && e.TryGetProperty("toStatus", out var ts)
                && ts.GetString() == target);
    }

    [Fact]
    public async Task Patch_status_unchanged_returns_bad_request()
    {
        var token = await LoginAdminAsync();
        var target = await PickDifferentStatusAsync(token);
        using (var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status"))
        {
            patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            patch.Content = JsonContent.Create(new { status = target, reason = "Primeira alteração" });
            var first = await _client.SendAsync(patch);
            Assert.Equal(HttpStatusCode.NoContent, first.StatusCode);
        }

        using (var patch2 = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status"))
        {
            patch2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            patch2.Content = JsonContent.Create(new { status = target, reason = "Repetir mesmo status" });
            var second = await _client.SendAsync(patch2);
            Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        }
    }

    private async Task<string> PickDifferentStatusAsync(string token)
    {
        using var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getReq);
        getRes.EnsureSuccessStatusCode();
        var detail = await getRes.Content.ReadFromJsonAsync<JsonElement>();
        var current = detail.GetProperty("status").GetString() ?? "NaoAssociado";
        return current == "Ativo" ? "Inadimplente" : "Ativo";
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

    private async Task<string> LoginTorcedorAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.False(string.IsNullOrEmpty(tokens?.AccessToken));
        return tokens!.AccessToken;
    }

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
