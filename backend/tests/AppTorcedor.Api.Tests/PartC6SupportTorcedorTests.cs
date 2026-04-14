using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartC6SupportTorcedorTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_support_tickets_requires_authentication()
    {
        var res = await _client.GetAsync("/api/support/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Torcedor_support_roundtrip_create_list_get_reply_cancel_reopen_attachment_download()
    {
        var token = await LoginTorcedorAsync();
        var userId = await GetUserIdFromMeAsync(token);

        Guid ticketId;
        using (var mp = new MultipartFormDataContent())
        {
            mp.Add(new StringContent("Geral"), "queue");
            mp.Add(new StringContent("Preciso de ajuda"), "subject");
            mp.Add(new StringContent("Normal"), "priority");
            mp.Add(new StringContent("Mensagem inicial"), "initialMessage");
            var pdfBytes = Encoding.UTF8.GetBytes("%PDF-1.4\n1 0 obj<<>>endobj\ntrailer<<>>\n%%EOF\n");
            var file = new ByteArrayContent(pdfBytes);
            file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            mp.Add(file, "attachments", "doc.pdf");

            using var post = new HttpRequestMessage(HttpMethod.Post, "/api/support/tickets") { Content = mp };
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var list = new HttpRequestMessage(HttpMethod.Get, "/api/support/tickets?pageSize=50"))
        {
            list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(list);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Contains(
                page.GetProperty("items").EnumerateArray(),
                x => x.GetProperty("ticketId").GetString() == ticketId.ToString());
        }

        string? downloadUrl;
        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/support/tickets/{ticketId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Open", d.GetProperty("status").GetString());
            var messages = d.GetProperty("messages").EnumerateArray().ToList();
            Assert.True(messages.Count >= 1);
            var atts = messages[0].GetProperty("attachments").EnumerateArray().ToList();
            Assert.True(atts.Count >= 1);
            downloadUrl = atts[0].GetProperty("downloadUrl").GetString();
            Assert.False(string.IsNullOrEmpty(downloadUrl));
        }

        using (var dl = new HttpRequestMessage(HttpMethod.Get, downloadUrl!))
        {
            dl.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(dl);
            res.EnsureSuccessStatusCode();
            Assert.Equal("application/pdf", res.Content.Headers.ContentType?.MediaType);
        }

        using (var reply = new HttpRequestMessage(HttpMethod.Post, $"/api/support/tickets/{ticketId}/reply"))
        {
            reply.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var mp = new MultipartFormDataContent();
            mp.Add(new StringContent("Resposta do torcedor"), "body");
            reply.Content = mp;
            var res = await _client.SendAsync(reply);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var cancel = new HttpRequestMessage(HttpMethod.Post, $"/api/support/tickets/{ticketId}/cancel"))
        {
            cancel.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(cancel);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var reopen = new HttpRequestMessage(HttpMethod.Post, $"/api/support/tickets/{ticketId}/reopen"))
        {
            reopen.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(reopen);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get2 = new HttpRequestMessage(HttpMethod.Get, $"/api/support/tickets/{ticketId}"))
        {
            get2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get2);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Open", d.GetProperty("status").GetString());
        }

        Assert.NotEqual(Guid.Empty, userId);
    }

    [Fact]
    public async Task Get_ticket_owned_by_another_user_returns_404()
    {
        var adminToken = await LoginAdminAsync();
        Guid otherTicketId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/support/tickets"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            post.Content = JsonContent.Create(
                new
                {
                    requesterUserId = TestingSeedConstants.SampleMemberUserId,
                    queue = "Geral",
                    subject = "Outro",
                    priority = "Normal",
                    initialMessage = "Interno",
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            otherTicketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        var torcedorToken = await LoginTorcedorAsync();
        using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/support/tickets/{otherTicketId}");
        get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedorToken);
        var res2 = await _client.SendAsync(get);
        Assert.Equal(HttpStatusCode.NotFound, res2.StatusCode);
    }

    [Fact]
    public async Task Admin_can_download_attachment_via_admin_route()
    {
        var torcedorToken = await LoginTorcedorAsync();
        Guid ticketId;
        Guid attachmentId;
        using (var mp = new MultipartFormDataContent())
        {
            mp.Add(new StringContent("Geral"), "queue");
            mp.Add(new StringContent("Com anexo"), "subject");
            mp.Add(new StringContent("Normal"), "priority");
            mp.Add(new StringContent("Olá"), "initialMessage");
            var png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");
            var file = new ByteArrayContent(png);
            file.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            mp.Add(file, "attachments", "dot.png");

            using var post = new HttpRequestMessage(HttpMethod.Post, "/api/support/tickets") { Content = mp };
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedorToken);
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/support/tickets/{ticketId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedorToken);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            attachmentId = d.GetProperty("messages")[0].GetProperty("attachments")[0].GetProperty("attachmentId").GetGuid();
        }

        var adminToken = await LoginAdminAsync();
        using (var dl = new HttpRequestMessage(
 HttpMethod.Get,
                   $"/api/admin/support/tickets/{ticketId}/attachments/{attachmentId}"))
        {
            dl.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var res = await _client.SendAsync(dl);
            res.EnsureSuccessStatusCode();
            Assert.Equal("image/png", res.Content.Headers.ContentType?.MediaType);
        }
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

    private async Task<Guid> GetUserIdFromMeAsync(string accessToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var me = await res.Content.ReadFromJsonAsync<JsonElement>();
        return Guid.Parse(me.GetProperty("id").GetString()!);
    }

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
