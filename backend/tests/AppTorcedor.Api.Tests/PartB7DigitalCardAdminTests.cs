using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

[Collection("DigitalCardAdmin")]
public sealed class PartB7DigitalCardAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_digital_cards_requires_carteirinha_visualizar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/digital-cards");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Issue_when_membership_not_active_returns_bad_request()
    {
        var token = await LoginAdminAsync();
        await PrepareSampleMembershipAsync(token, "NaoAssociado");
        using var issue = new HttpRequestMessage(HttpMethod.Post, "/api/admin/digital-cards/issue");
        issue.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        issue.Content = JsonContent.Create(new { membershipId = TestingSeedConstants.SampleMembershipId });
        var res = await _client.SendAsync(issue);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Issue_regenerate_invalidate_happy_path_and_conflict()
    {
        var token = await LoginAdminAsync();
        await PrepareSampleMembershipAsync(token, "Ativo");

        using (var issue = new HttpRequestMessage(HttpMethod.Post, "/api/admin/digital-cards/issue"))
        {
            issue.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            issue.Content = JsonContent.Create(new { membershipId = TestingSeedConstants.SampleMembershipId });
            var firstIssue = await _client.SendAsync(issue);
            Assert.Equal(HttpStatusCode.NoContent, firstIssue.StatusCode);
        }

        using (var issue2 = new HttpRequestMessage(HttpMethod.Post, "/api/admin/digital-cards/issue"))
        {
            issue2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            issue2.Content = JsonContent.Create(new { membershipId = TestingSeedConstants.SampleMembershipId });
            var dup = await _client.SendAsync(issue2);
            Assert.Equal(HttpStatusCode.Conflict, dup.StatusCode);
        }

        Guid activeCardId;
        using (var listReq = new HttpRequestMessage(
                   HttpMethod.Get,
                   $"/api/admin/digital-cards?membershipId={TestingSeedConstants.SampleMembershipId}&status=Active"))
        {
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var listRes = await _client.SendAsync(listReq);
            listRes.EnsureSuccessStatusCode();
            var listBody = await listRes.Content.ReadFromJsonAsync<JsonElement>();
            var items = listBody.GetProperty("items");
            Assert.Equal(JsonValueKind.Array, items.ValueKind);
            Assert.Single(items.EnumerateArray());
            activeCardId = Guid.Parse(items[0].GetProperty("digitalCardId").GetString()!);
            Assert.Equal(1, items[0].GetProperty("version").GetInt32());
        }

        using (var getDetail = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/digital-cards/{activeCardId}"))
        {
            getDetail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var detailRes = await _client.SendAsync(getDetail);
            detailRes.EnsureSuccessStatusCode();
            var detail = await detailRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(detail.TryGetProperty("templatePreviewLines", out var lines));
            Assert.Equal(JsonValueKind.Array, lines.ValueKind);
            Assert.NotEmpty(lines.EnumerateArray());
        }

        using (var regen = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/digital-cards/{activeCardId}/regenerate"))
        {
            regen.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            regen.Content = JsonContent.Create(new { reason = "Troca de documento" });
            var regenRes = await _client.SendAsync(regen);
            Assert.Equal(HttpStatusCode.NoContent, regenRes.StatusCode);
        }

        using (var listReq2 = new HttpRequestMessage(
                   HttpMethod.Get,
                   $"/api/admin/digital-cards?membershipId={TestingSeedConstants.SampleMembershipId}&status=Active"))
        {
            listReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var listRes2 = await _client.SendAsync(listReq2);
            listRes2.EnsureSuccessStatusCode();
            var listBody2 = await listRes2.Content.ReadFromJsonAsync<JsonElement>();
            var activeItems = listBody2.GetProperty("items");
            Assert.Single(activeItems.EnumerateArray());
            Assert.Equal(2, activeItems[0].GetProperty("version").GetInt32());
        }

        Guid secondActiveId;
        using (var listActive = new HttpRequestMessage(
                   HttpMethod.Get,
                   $"/api/admin/digital-cards?membershipId={TestingSeedConstants.SampleMembershipId}&status=Active"))
        {
            listActive.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(listActive);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            secondActiveId = Guid.Parse(body.GetProperty("items")[0].GetProperty("digitalCardId").GetString()!);
        }

        using (var inv = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/digital-cards/{secondActiveId}/invalidate"))
        {
            inv.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            inv.Content = JsonContent.Create(new { reason = "Suspeita de fraude" });
            var invRes = await _client.SendAsync(inv);
            Assert.Equal(HttpStatusCode.NoContent, invRes.StatusCode);
        }

        using (var listReq3 = new HttpRequestMessage(
                   HttpMethod.Get,
                   $"/api/admin/digital-cards?membershipId={TestingSeedConstants.SampleMembershipId}&status=Active"))
        {
            listReq3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var listRes3 = await _client.SendAsync(listReq3);
            listRes3.EnsureSuccessStatusCode();
            var listBody3 = await listRes3.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Empty(listBody3.GetProperty("items").EnumerateArray());
        }

        using var auditReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/audit-logs?entityType=DigitalCardRecord&take=30");
        auditReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var auditRes = await _client.SendAsync(auditReq);
        auditRes.EnsureSuccessStatusCode();
        var logs = await auditRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Contains(
            logs.EnumerateArray(),
            e => e.TryGetProperty("entityType", out var et) && et.GetString() == "DigitalCardRecord");
    }

    [Fact]
    public async Task Invalidate_without_reason_returns_bad_request()
    {
        var token = await LoginAdminAsync();
        await PrepareSampleMembershipAsync(token, "Ativo");
        using (var issue = new HttpRequestMessage(HttpMethod.Post, "/api/admin/digital-cards/issue"))
        {
            issue.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            issue.Content = JsonContent.Create(new { membershipId = TestingSeedConstants.SampleMembershipId });
            var firstIssue = await _client.SendAsync(issue);
            Assert.Equal(HttpStatusCode.NoContent, firstIssue.StatusCode);
        }

        Guid cardId;
        using (var listReq = new HttpRequestMessage(
                   HttpMethod.Get,
                   $"/api/admin/digital-cards?membershipId={TestingSeedConstants.SampleMembershipId}&status=Active"))
        {
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var listRes = await _client.SendAsync(listReq);
            listRes.EnsureSuccessStatusCode();
            var listBody = await listRes.Content.ReadFromJsonAsync<JsonElement>();
            cardId = Guid.Parse(listBody.GetProperty("items")[0].GetProperty("digitalCardId").GetString()!);
        }

        using var inv = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/digital-cards/{cardId}/invalidate");
        inv.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        inv.Content = JsonContent.Create(new { reason = (string?)null });
        var invRes = await _client.SendAsync(inv);
        Assert.Equal(HttpStatusCode.BadRequest, invRes.StatusCode);
    }

    private async Task PrepareSampleMembershipAsync(string token, string targetStatus)
    {
        await CleanupActiveDigitalCardsForSampleMembershipAsync(token);
        var current = await GetSampleMembershipStatusAsync(token);
        if (current == targetStatus)
            return;
        using var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/memberships/{TestingSeedConstants.SampleMembershipId}/status");
        patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        patch.Content = JsonContent.Create(new { status = targetStatus, reason = "Preparação teste B.7" });
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
                inv.Content = JsonContent.Create(new { reason = "Limpeza teste B.7" });
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
