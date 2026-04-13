using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartC4TorcedorGamesTicketsTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Games_list_requires_auth()
    {
        var res = await _client.GetAsync("/api/games");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Tickets_list_requires_auth()
    {
        var res = await _client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Torcedor_list_shows_only_active_games()
    {
        var admin = await LoginAdminAsync();
        var member = await LoginMemberAsync();
        var opponentActive = $"C4A-{Guid.NewGuid():N}"[..10];
        var opponentInactive = $"C4I-{Guid.NewGuid():N}"[..10];

        Guid activeId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = opponentActive,
                    competition = "Brasileirão",
                    gameDate = DateTimeOffset.UtcNow.AddDays(10),
                    isActive = true,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            activeId = Guid.Parse(body.GetProperty("gameId").GetString()!);
        }

        Guid inactiveId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = opponentInactive,
                    competition = "Copa",
                    gameDate = DateTimeOffset.UtcNow.AddDays(11),
                    isActive = true,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            inactiveId = Guid.Parse(body.GetProperty("gameId").GetString()!);
        }

        using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/games/{inactiveId}"))
        {
            del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            (await _client.SendAsync(del)).EnsureSuccessStatusCode();
        }

        using var list = new HttpRequestMessage(HttpMethod.Get, $"/api/games?search=C4&pageSize=50");
        list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
        var listRes = await _client.SendAsync(list);
        listRes.EnsureSuccessStatusCode();
        var page = await listRes.Content.ReadFromJsonAsync<JsonElement>();
        var ids = new HashSet<string>();
        foreach (var el in page.GetProperty("items").EnumerateArray())
            ids.Add(el.GetProperty("gameId").GetString()!);
        Assert.Contains(activeId.ToString(), ids);
        Assert.DoesNotContain(inactiveId.ToString(), ids);
    }

    [Fact]
    public async Task Member_lists_gets_detail_and_redeems_purchased_ticket()
    {
        var admin = await LoginAdminAsync();
        var member = await LoginMemberAsync();

        Guid gameId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = "C4Final",
                    competition = "Taça",
                    gameDate = DateTimeOffset.UtcNow.AddDays(4),
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
            reserve.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            reserve.Content = JsonContent.Create(
                new { userId = TestingSeedConstants.SampleMemberUserId, gameId });
            var res = await _client.SendAsync(reserve);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var pur = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/tickets/{ticketId}/purchase"))
        {
            pur.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            (await _client.SendAsync(pur)).EnsureSuccessStatusCode();
        }

        using (var list = new HttpRequestMessage(HttpMethod.Get, "/api/tickets?pageSize=50"))
        {
            list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
            var res = await _client.SendAsync(list);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            var found = false;
            foreach (var el in page.GetProperty("items").EnumerateArray())
            {
                if (el.GetProperty("ticketId").GetString() == ticketId.ToString())
                    found = true;
            }
            Assert.True(found);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/tickets/{ticketId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Purchased", d.GetProperty("status").GetString());
        }

        using (var red = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{ticketId}/redeem"))
        {
            red.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
            var res = await _client.SendAsync(red);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get2 = new HttpRequestMessage(HttpMethod.Get, $"/api/tickets/{ticketId}"))
        {
            get2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
            var res = await _client.SendAsync(get2);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Redeemed", d.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task Other_user_gets_not_found_for_member_ticket()
    {
        var admin = await LoginAdminAsync();
        var member = await LoginMemberAsync();
        var other = await LoginTorcedorAsync();

        Guid gameId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = "C4Iso",
                    competition = "Liga",
                    gameDate = DateTimeOffset.UtcNow.AddDays(5),
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
            reserve.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            reserve.Content = JsonContent.Create(
                new { userId = TestingSeedConstants.SampleMemberUserId, gameId });
            var res = await _client.SendAsync(reserve);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/tickets/{ticketId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other);
            var res = await _client.SendAsync(get);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        using (var red = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{ticketId}/redeem"))
        {
            red.Headers.Authorization = new AuthenticationHeaderValue("Bearer", other);
            var res = await _client.SendAsync(red);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        using (var getMember = new HttpRequestMessage(HttpMethod.Get, $"/api/tickets/{ticketId}"))
        {
            getMember.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
            var res = await _client.SendAsync(getMember);
            res.EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task Member_redeem_reserved_ticket_returns_bad_request()
    {
        var admin = await LoginAdminAsync();
        var member = await LoginMemberAsync();

        Guid gameId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/games"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    opponent = "C4Res",
                    competition = "Camp",
                    gameDate = DateTimeOffset.UtcNow.AddDays(2),
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
            reserve.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            reserve.Content = JsonContent.Create(
                new { userId = TestingSeedConstants.SampleMemberUserId, gameId });
            var res = await _client.SendAsync(reserve);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            ticketId = Guid.Parse(body.GetProperty("ticketId").GetString()!);
        }

        using (var red = new HttpRequestMessage(HttpMethod.Post, $"/api/tickets/{ticketId}/redeem"))
        {
            red.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
            var res = await _client.SendAsync(red);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
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
