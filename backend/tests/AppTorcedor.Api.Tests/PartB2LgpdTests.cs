using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB2LgpdTests
{
    [Fact]
    public async Task Lgpd_admin_flow_documents_consent_export_and_torcedor_forbidden()
    {
        using var factory = new AppWebApplicationFactory();
        var client = factory.CreateClient();
        var adminToken = await LoginAdminAsync(client);

        using (var listReq = new HttpRequestMessage(HttpMethod.Get, "/api/admin/lgpd/documents"))
        {
            listReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var listRes = await client.SendAsync(listReq);
            Assert.Equal(HttpStatusCode.OK, listRes.StatusCode);
            var empty = await listRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, empty.ValueKind);
            Assert.Equal(0, empty.GetArrayLength());
        }

        Guid docId;
        Guid versionId;
        using (var createDoc = new HttpRequestMessage(HttpMethod.Post, "/api/admin/lgpd/documents"))
        {
            createDoc.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            createDoc.Content = JsonContent.Create(new { type = "TermsOfUse", title = "Termos de uso" });
            var createRes = await client.SendAsync(createDoc);
            Assert.Equal(HttpStatusCode.OK, createRes.StatusCode);
            var doc = await createRes.Content.ReadFromJsonAsync<JsonElement>();
            docId = doc!.GetProperty("id").GetGuid();
 }

        using (var dup = new HttpRequestMessage(HttpMethod.Post, "/api/admin/lgpd/documents"))
        {
            dup.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            dup.Content = JsonContent.Create(new { type = "TermsOfUse", title = "Dup" });
            var dupRes = await client.SendAsync(dup);
            Assert.Equal(HttpStatusCode.BadRequest, dupRes.StatusCode);
        }

        using (var addVer = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/lgpd/documents/{docId}/versions"))
        {
            addVer.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            addVer.Content = JsonContent.Create(new { content = "Texto legal v1" });
            var vRes = await client.SendAsync(addVer);
            Assert.Equal(HttpStatusCode.OK, vRes.StatusCode);
            var ver = await vRes.Content.ReadFromJsonAsync<JsonElement>();
            versionId = ver!.GetProperty("id").GetGuid();
            Assert.Equal("Draft", ver.GetProperty("status").GetString());
        }

        using (var pub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/lgpd/legal-document-versions/{versionId}/publish"))
        {
            pub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var pubRes = await client.SendAsync(pub);
            Assert.Equal(HttpStatusCode.NoContent, pubRes.StatusCode);
        }

        var torcedorLogin = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, torcedorLogin.StatusCode);
        var torcedorTokens = await torcedorLogin.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.NotNull(torcedorTokens);

        using (var forbid = new HttpRequestMessage(HttpMethod.Get, "/api/admin/lgpd/documents"))
        {
            forbid.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedorTokens!.AccessToken);
            var forbidRes = await client.SendAsync(forbid);
            Assert.Equal(HttpStatusCode.Forbidden, forbidRes.StatusCode);
        }

        Guid torcedorUserId;
        using (var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me"))
        {
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedorTokens.AccessToken);
            var meRes = await client.SendAsync(meReq);
            var me = await meRes.Content.ReadFromJsonAsync<MeDto>();
            Assert.NotNull(me);
            torcedorUserId = me!.Id;
        }

        using (var consent = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/lgpd/users/{torcedorUserId}/consents"))
        {
            consent.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            consent.Content = JsonContent.Create(new { documentVersionId = versionId, clientIp = (string?)null });
            var cRes = await client.SendAsync(consent);
            Assert.Equal(HttpStatusCode.NoContent, cRes.StatusCode);
        }

        using (var listC = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/lgpd/users/{torcedorUserId}/consents"))
        {
            listC.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var listCRes = await client.SendAsync(listC);
            Assert.Equal(HttpStatusCode.OK, listCRes.StatusCode);
            var arr = await listCRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(1, arr.GetArrayLength());
        }

        using (var export = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/lgpd/users/{torcedorUserId}/export"))
        {
            export.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var exRes = await client.SendAsync(export);
            Assert.Equal(HttpStatusCode.OK, exRes.StatusCode);
            var exBody = await exRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Completed", exBody!.GetProperty("status").GetString());
            Assert.False(string.IsNullOrEmpty(exBody.GetProperty("resultJson").GetString()));
            Assert.Contains(TestingSeedConstants.TorcedorEmail, exBody.GetProperty("resultJson").GetString() ?? "");
        }

        using (var anon = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/lgpd/users/{torcedorUserId}/anonymize"))
        {
            anon.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var anonRes = await client.SendAsync(anon);
            Assert.Equal(HttpStatusCode.OK, anonRes.StatusCode);
            var anonBody = await anonRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Completed", anonBody!.GetProperty("status").GetString());
        }

        var loginAfter = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.Unauthorized, loginAfter.StatusCode);
    }

    [Fact]
    public async Task Me_includes_lgpd_permissions_for_admin_master()
    {
        using var factory = new AppWebApplicationFactory();
        var client = factory.CreateClient();
        var token = await LoginAdminAsync(client);
        using var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var meRes = await client.SendAsync(meReq);
        var me = await meRes.Content.ReadFromJsonAsync<MeDto>();
        Assert.NotNull(me);
        Assert.Contains(ApplicationPermissions.LgpdDocumentosVisualizar, me!.Permissions);
        Assert.Contains(ApplicationPermissions.LgpdDadosExportar, me.Permissions);
    }

    private static async Task<string> LoginAdminAsync(HttpClient client)
    {
        var login = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokens>();
        Assert.False(string.IsNullOrEmpty(tokens?.AccessToken));
        return tokens!.AccessToken;
    }

    private sealed record AuthTokens(string AccessToken);

    private sealed record MeDto(Guid Id, IReadOnlyList<string> Permissions);
}
