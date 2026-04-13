using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB11SupportAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_support_tickets_requires_chamados_responder()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/support/tickets");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task List_support_tickets_ok_for_admin_master()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/support/tickets?pageSize=50");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Support_ticket_create_get_reply_assign_status_history_roundtrip()
    {
        var token = await LoginAdminAsync();
        var adminId = await GetAdminUserIdAsync(token);

        Guid ticketId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/support/tickets"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    requesterUserId = TestingSeedConstants.SampleMemberUserId,
                    queue = "Geral",
                    subject = "Dúvida plano",
                    priority = "Normal",
                    initialMessage = "Preciso de ajuda",
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/support/tickets/{ticketId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Open", d.GetProperty("status").GetString());
            Assert.True(d.GetProperty("messages").GetArrayLength() >= 1);
            Assert.Contains(
                d.GetProperty("history").EnumerateArray(),
                h => h.GetProperty("eventType").GetString() == "Created");
        }

        using (var reply = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/reply"))
        {
            reply.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            reply.Content = JsonContent.Create(new { body = "Resposta do atendente", isInternal = false });
            var res = await _client.SendAsync(reply);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var assign = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/assign"))
        {
            assign.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            assign.Content = JsonContent.Create(new { agentUserId = adminId });
            var res = await _client.SendAsync(assign);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var st = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/status"))
        {
            st.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            st.Content = JsonContent.Create(new { status = "InProgress", reason = "Em análise" });
            var res = await _client.SendAsync(st);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get2 = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/support/tickets/{ticketId}"))
        {
            get2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get2);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("InProgress", d.GetProperty("status").GetString());
            Assert.True(d.GetProperty("history").GetArrayLength() >= 3);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var openCount = await db.SupportTickets.AsNoTracking()
                .CountAsync(
                    t => t.Status == SupportTicketStatus.Open
                        || t.Status == SupportTicketStatus.InProgress
                        || t.Status == SupportTicketStatus.WaitingUser);
            Assert.True(openCount >= 1);
        }

        using (var dash = new HttpRequestMessage(HttpMethod.Get, "/api/admin/dashboard"))
        {
            dash.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var dRes = await _client.SendAsync(dash);
            dRes.EnsureSuccessStatusCode();
            var dashBody = await dRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(dashBody.GetProperty("openSupportTickets").GetInt32() >= 1);
        }

        using (var toResolved = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/status"))
        {
            toResolved.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            toResolved.Content = JsonContent.Create(new { status = "Resolved", reason = "ok" });
            (await _client.SendAsync(toResolved)).EnsureSuccessStatusCode();
        }

        using (var bad = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/status"))
        {
            bad.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            bad.Content = JsonContent.Create(new { status = "WaitingUser", reason = "inválido" });
            var res = await _client.SendAsync(bad);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }
    }

    [Fact]
    public async Task Reply_to_closed_ticket_returns_conflict()
    {
        var token = await LoginAdminAsync();
        Guid ticketId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/support/tickets"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    requesterUserId = TestingSeedConstants.SampleMemberUserId,
                    queue = "Geral",
                    subject = "Fechar",
                    priority = "Normal",
                    initialMessage = (string?)null,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        foreach (var status in new[] { "InProgress", "Resolved", "Closed" })
        {
            using var st = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/status");
            st.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            st.Content = JsonContent.Create(new { status, reason = (string?)null });
            (await _client.SendAsync(st)).EnsureSuccessStatusCode();
        }

        using (var reply = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/support/tickets/{ticketId}/reply"))
        {
            reply.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            reply.Content = JsonContent.Create(new { body = "tarde", isInternal = false });
            var res = await _client.SendAsync(reply);
            Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
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

    private async Task<Guid> GetAdminUserIdAsync(string accessToken)
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
