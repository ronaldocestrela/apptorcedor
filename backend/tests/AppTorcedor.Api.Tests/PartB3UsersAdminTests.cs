using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB3UsersAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Torcedor_cannot_list_admin_users()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(tokens);

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_lists_users_search_by_document_and_can_update_profile_and_audit()
    {
        var adminToken = await LoginAdminAsync();

        using (var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/users?search=12345678901"))
        {
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var listRes = await _client.SendAsync(listReq);
            Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
            var page = await listRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(page.GetProperty("totalCount").GetInt32() >= 1);
            var items = page.GetProperty("items");
            Assert.Contains(
                items.EnumerateArray(),
                e =>
                    e.GetProperty("email").GetString() == TestingSeedConstants.MemberEmail
                    && e.GetProperty("document").GetString() == "12345678901");
        }

        using (var detailReq = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/users/{TestingSeedConstants.SampleMemberUserId}"))
        {
            detailReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var detailRes = await _client.SendAsync(detailReq);
            Assert.Equal(HttpStatusCode.OK, detailRes.StatusCode);
            var detail = await detailRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(TestingSeedConstants.MemberEmail, detail.GetProperty("email").GetString());
            Assert.Equal(JsonValueKind.Object, detail.GetProperty("membership").ValueKind);
            Assert.Equal("12345678901", detail.GetProperty("profile").GetProperty("document").GetString());
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/users/{TestingSeedConstants.SampleMemberUserId}/profile"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            put.Content = JsonContent.Create(
                new
                {
                    document = "12345678901",
                    birthDate = (DateOnly?)new DateOnly(1995, 5, 15),
                    photoUrl = (string?)"https://example.com/photo.jpg",
                    address = "Novo endereço",
                    administrativeNote = "Cliente VIP",
                });
            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);
        }

        using (var auditReq = new HttpRequestMessage(
            HttpMethod.Get,
            $"/api/admin/users/{TestingSeedConstants.SampleMemberUserId}/audit-logs?take=20"))
        {
            auditReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var auditRes = await _client.SendAsync(auditReq);
            Assert.Equal(HttpStatusCode.OK, auditRes.StatusCode);
            var logs = await auditRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, logs.ValueKind);
            Assert.Contains(
                logs.EnumerateArray(),
                row => row.GetProperty("entityType").GetString() == "UserProfileRecord");
        }
    }

    [Fact]
    public async Task Admin_can_deactivate_torcedor_account_blocking_login()
    {
        var adminToken = await LoginAdminAsync();

        var torcedorLogin = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, torcedorLogin.StatusCode);
        var torcedorTokens = await torcedorLogin.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(torcedorTokens);

        Guid torcedorId;
        using (var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me"))
        {
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedorTokens!.AccessToken);
            var meRes = await _client.SendAsync(meReq);
            var me = await meRes.Content.ReadFromJsonAsync<MeDto>();
            Assert.NotNull(me);
            torcedorId = me!.Id;
        }

        using (var patch = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/users/{torcedorId}/active"))
        {
            patch.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            patch.Content = JsonContent.Create(new { isActive = false });
            var patched = await _client.SendAsync(patch);
            Assert.Equal(HttpStatusCode.NoContent, patched.StatusCode);
        }

        var loginAfter = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfter.StatusCode);

        using (var patchOn = new HttpRequestMessage(HttpMethod.Patch, $"/api/admin/users/{torcedorId}/active"))
        {
            patchOn.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            patchOn.Content = JsonContent.Create(new { isActive = true });
            var patchedOn = await _client.SendAsync(patchOn);
            Assert.Equal(HttpStatusCode.NoContent, patchedOn.StatusCode);
        }

        var loginRestored = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, loginRestored.StatusCode);
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

    private sealed record MeDto(Guid Id);
}
