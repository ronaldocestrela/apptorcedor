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

public sealed class PartB9NewsAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_news_requires_noticias_publicar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/news");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task List_news_returns_ok_for_admin_master()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/news?pageSize=50");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task News_create_update_publish_unpublish_notify_roundtrip()
    {
        var token = await LoginAdminAsync();
        Guid newsId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/news"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    title = "Título",
                    summary = "Resumo",
                    content = "Corpo",
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            newsId = Guid.Parse(body.GetProperty("newsId").GetString()!);
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/news/{newsId}"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            put.Content = JsonContent.Create(
                new
                {
                    title = "Título 2",
                    summary = "Resumo 2",
                    content = "Corpo 2",
                });
            var res = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/news/{newsId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Título 2", d.GetProperty("title").GetString());
            Assert.Equal("Draft", d.GetProperty("status").GetString());
        }

        using (var notifyDraft = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{newsId}/notifications"))
        {
            notifyDraft.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            notifyDraft.Content = JsonContent.Create(new { scheduledAt = (DateTimeOffset?)null, userIds = (Guid[]?)null });
            var res = await _client.SendAsync(notifyDraft);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }

        using (var pub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{newsId}/publish"))
        {
            pub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(pub);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var notify = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{newsId}/notifications"))
        {
            notify.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            notify.Content = JsonContent.Create(new { scheduledAt = (DateTimeOffset?)null, userIds = (Guid[]?)null });
            var res = await _client.SendAsync(notify);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var n = await db.InAppNotifications.AsNoTracking().CountAsync(x => x.NewsArticleId == newsId);
            Assert.True(n >= 1);
        }

        using (var unpub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{newsId}/unpublish"))
        {
            unpub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(unpub);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }
    }

    [Fact]
    public async Task Scheduled_notifications_processed_by_dispatch_service()
    {
        var token = await LoginAdminAsync();
        Guid newsId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/news"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new { title = "Agendada", summary = "S", content = "C" });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            newsId = Guid.Parse(body.GetProperty("newsId").GetString()!);
        }

        using (var pub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{newsId}/publish"))
        {
            pub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            (await _client.SendAsync(pub)).EnsureSuccessStatusCode();
        }

        var future = DateTimeOffset.UtcNow.AddHours(2);
        using (var notify = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{newsId}/notifications"))
        {
            notify.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            notify.Content = JsonContent.Create(new { scheduledAt = future, userIds = (Guid[]?)null });
            (await _client.SendAsync(notify)).EnsureSuccessStatusCode();
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var pending = await db.InAppNotifications.Where(x => x.NewsArticleId == newsId && x.Status == InAppNotificationStatus.Pending).ToListAsync();
            Assert.NotEmpty(pending);
            foreach (var row in pending)
                row.ScheduledAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();

            var dispatch = scope.ServiceProvider.GetRequiredService<IInAppNotificationDispatchService>();
            var processed = await dispatch.ProcessDueAsync(CancellationToken.None);
            Assert.True(processed > 0);

            var stillPending = await db.InAppNotifications.AsNoTracking().CountAsync(x => x.NewsArticleId == newsId && x.Status == InAppNotificationStatus.Pending);
            Assert.Equal(0, stillPending);
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
