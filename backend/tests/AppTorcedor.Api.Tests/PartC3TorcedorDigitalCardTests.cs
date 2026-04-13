using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

[Collection("DigitalCardAdmin")]
public sealed class PartC3TorcedorDigitalCardTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Digital_card_requires_auth()
    {
        var res = await _client.GetAsync("/api/account/digital-card");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Member_not_associated_gets_not_associated_state()
    {
        var admin = await LoginAdminAsync();
        await PrepareSampleMembershipAsync(admin, "NaoAssociado");
        var memberToken = await LoginMemberAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/account/digital-card");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nameof(MyDigitalCardViewState.NotAssociated), body.GetProperty("state").GetString());
        Assert.Equal(TestingSeedConstants.SampleMembershipId.ToString(), body.GetProperty("membershipId").GetString());
        Assert.True(body.GetProperty("message").GetString()!.Length > 0);
    }

    [Fact]
    public async Task Member_active_without_card_gets_awaiting_issuance()
    {
        var admin = await LoginAdminAsync();
        await PrepareSampleMembershipAsync(admin, "Ativo");
        var memberToken = await LoginMemberAsync();

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/account/digital-card");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nameof(MyDigitalCardViewState.AwaitingIssuance), body.GetProperty("state").GetString());
        Assert.False(body.TryGetProperty("digitalCardId", out var d) && d.ValueKind != JsonValueKind.Null);
    }

    [Fact]
    public async Task Member_active_with_issued_card_gets_active_payload()
    {
        var admin = await LoginAdminAsync();
        await PrepareSampleMembershipAsync(admin, "Ativo");
        using (var issue = new HttpRequestMessage(HttpMethod.Post, "/api/admin/digital-cards/issue"))
        {
            issue.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            issue.Content = JsonContent.Create(new { membershipId = TestingSeedConstants.SampleMembershipId });
            var issueRes = await _client.SendAsync(issue);
            Assert.Equal(HttpStatusCode.NoContent, issueRes.StatusCode);
        }

        var memberToken = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/account/digital-card");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nameof(MyDigitalCardViewState.Active), body.GetProperty("state").GetString());
        Assert.True(body.TryGetProperty("digitalCardId", out var cid) && Guid.TryParse(cid.GetString(), out _));
        Assert.True(body.TryGetProperty("verificationToken", out var tok) && tok.GetString()!.Length > 8);
        Assert.True(body.TryGetProperty("templatePreviewLines", out var lines) && lines.ValueKind == JsonValueKind.Array);
        Assert.NotEmpty(lines.EnumerateArray());
        Assert.True(body.TryGetProperty("cacheValidUntilUtc", out var cache) && cache.ValueKind == JsonValueKind.String);
    }

    [Fact]
    public async Task Torcedor_without_membership_row_gets_not_associated_without_membership_id()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/account/digital-card");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(nameof(MyDigitalCardViewState.NotAssociated), body.GetProperty("state").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("membershipId").ValueKind);
    }

    private async Task PrepareSampleMembershipAsync(string token, string targetStatus)
    {
        await CleanupActiveDigitalCardsForSampleMembershipAsync(token);
        var current = await GetSampleMembershipStatusAsync(token);
        if (current == targetStatus)
            return;
        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status");
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        patch.Content = JsonContent.Create(new { status = targetStatus, reason = "Preparação teste C.3" });
        var res = await _client.SendAsync(patch);
        res.EnsureSuccessStatusCode();
    }

    private async Task<string?> GetSampleMembershipStatusAsync(string token)
    {
        using var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}");
        getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var getRes = await _client.SendAsync(getReq);
        getRes.EnsureSuccessStatusCode();
        var body = await getRes.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("status").GetString();
    }

    private async Task CleanupActiveDigitalCardsForSampleMembershipAsync(string token)
    {
        while (true)
        {
            using var listReq = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/admin/digital-cards?membershipId={TestingSeedConstants.SampleMembershipId}&status=Active&pageSize=50");
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var listRes = await _client.SendAsync(listReq);
            listRes.EnsureSuccessStatusCode();
            var listBody = await listRes.Content.ReadFromJsonAsync<JsonElement>();
            var items = listBody.GetProperty("items");
            if (items.GetArrayLength() == 0)
                break;
            foreach (var row in items.EnumerateArray())
            {
                var id = row.GetProperty("digitalCardId").GetString();
                using var inv = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/digital-cards/{id}/invalidate");
                inv.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                inv.Content = JsonContent.Create(new { reason = "Limpeza teste C.3" });
                var invRes = await _client.SendAsync(inv);
                invRes.EnsureSuccessStatusCode();
            }
        }
    }

    private async Task<string> LoginAdminAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        return tokens!.AccessToken;
    }

    private async Task<string> LoginMemberAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.MemberEmail, password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        return tokens!.AccessToken;
    }

    private async Task<string> LoginTorcedorAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthResponseDto>();
        return tokens!.AccessToken;
    }

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
