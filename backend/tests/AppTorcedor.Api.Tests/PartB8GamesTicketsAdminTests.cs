using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB8GamesTicketsAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_games_requires_jogos_visualizar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/games");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task List_games_returns_ok_for_admin()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/games?pageSize=50");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Games_create_update_deactivate_roundtrip()
    {
        var token = await LoginAdminAsync();
        var opponent = $"Op-{Guid.NewGuid():N}"[..8];
        Guid gameId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    opponent,
                    competition = "Brasileirão",
                    gameDate = DateTimeOffset.UtcNow.AddDays(7),
                    isActive = true,
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            gameId = Guid.Parse(body.GetProperty("gameId").GetString()!);
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/games/{gameId}"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            put.Content = JsonContent.Create(
                new
                {
                    opponent = opponent + "X",
                    competition = "Copa",
                    gameDate = DateTimeOffset.UtcNow.AddDays(8),
                    isActive = true,
                });
            var res = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/games/{gameId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(opponent + "X", d.GetProperty("opponent").GetString());
        }

        using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/games/{gameId}"))
        {
            del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(del);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get2 = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/games/{gameId}"))
        {
            get2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get2);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(d.GetProperty("isActive").GetBoolean());
        }
    }

    [Fact]
    public async Task Reserve_on_inactive_game_returns_bad_request()
    {
        var token = await LoginAdminAsync();
        Guid gameId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = "Rival",
                    competition = "Camp",
                    gameDate = DateTimeOffset.UtcNow.AddDays(1),
                    isActive = true,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            gameId = Guid.Parse(body.GetProperty("gameId").GetString()!);
        }

        using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/games/{gameId}"))
        {
            del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            (await _client.SendAsync(del)).EnsureSuccessStatusCode();
        }

        using var reserve = new HttpRequestMessage(HttpMethod.Post, "/api/admin/tickets/reserve");
        reserve.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        reserve.Content = JsonContent.Create(
            new { userId = TestingSeedConstants.SampleMemberUserId, gameId });
        var r = await _client.SendAsync(reserve);
        Assert.Equal(HttpStatusCode.BadRequest, r.StatusCode);
    }

    [Fact]
    public async Task Tickets_reserve_purchase_sync_redeem_happy_path()
    {
        var token = await LoginAdminAsync();
        Guid gameId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = "Final",
                    competition = "Taca",
                    gameDate = DateTimeOffset.UtcNow.AddDays(3),
                    isActive = true,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            gameId = Guid.Parse(body.GetProperty("gameId").GetString()!);
        }

        Guid ticketId;
        using (var reserve = new HttpRequestMessage(HttpMethod.Post, "/api/admin/tickets/reserve"))
        {
            reserve.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            reserve.Content = JsonContent.Create(
                new { userId = TestingSeedConstants.SampleMemberUserId, gameId });
            var res = await _client.SendAsync(reserve);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var pur = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tickets/{ticketId}/purchase"))
        {
            pur.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(pur);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var sync = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tickets/{ticketId}/sync"))
        {
            sync.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(sync);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var red = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tickets/{ticketId}/redeem"))
        {
            red.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(red);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/tickets/{ticketId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Redeemed", d.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task List_tickets_requires_ingressos_visualizar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/tickets");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
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
