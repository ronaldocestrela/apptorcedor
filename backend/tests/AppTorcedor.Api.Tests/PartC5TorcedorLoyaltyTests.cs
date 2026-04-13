using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartC5TorcedorLoyaltyTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Loyalty_summary_requires_auth()
    {
        var res = await _client.GetAsync("/api/loyalty/me/summary");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Loyalty_rankings_all_time_requires_auth()
    {
        var res = await _client.GetAsync("/api/loyalty/rankings/all-time");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Member_sees_points_and_ranking_after_manual_adjustment()
    {
        var admin = await LoginAdminAsync();
        var member = await LoginMemberAsync();

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var torcedorId = await db.Users.AsNoTracking()
                .Where(u => u.Email == TestingSeedConstants.TorcedorEmail)
                .Select(u => u.Id)
                .SingleAsync();

            db.LoyaltyPointLedgerEntries.RemoveRange(
                db.LoyaltyPointLedgerEntries.Where(
                    e => e.UserId == TestingSeedConstants.SampleMemberUserId || e.UserId == torcedorId));
            await db.SaveChangesAsync();
        }

        using (var adjMember = new HttpRequestMessage(
 HttpMethod.Post,
                   $"/api/admin/loyalty/users/{TestingSeedConstants.SampleMemberUserId}/manual-adjustments"))
        {
            adjMember.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            adjMember.Content = JsonContent.Create(new { points = 100, reason = "C5 test member", campaignId = (Guid?)null });
            (await _client.SendAsync(adjMember)).EnsureSuccessStatusCode();
        }

        Guid torcedorUserId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            torcedorUserId = await db.Users.AsNoTracking()
                .Where(u => u.Email == TestingSeedConstants.TorcedorEmail)
                .Select(u => u.Id)
                .SingleAsync();
        }

        using (var adjTorcedor = new HttpRequestMessage(
                   HttpMethod.Post,
                   $"/api/admin/loyalty/users/{torcedorUserId}/manual-adjustments"))
        {
            adjTorcedor.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            adjTorcedor.Content = JsonContent.Create(new { points = 40, reason = "C5 test torcedor", campaignId = (Guid?)null });
            (await _client.SendAsync(adjTorcedor)).EnsureSuccessStatusCode();
        }

        using var sumReq = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/me/summary");
        sumReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
        var sumRes = await _client.SendAsync(sumReq);
        sumRes.EnsureSuccessStatusCode();
        var summary = await sumRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(100, summary.GetProperty("totalPoints").GetInt32());
        Assert.Equal(100, summary.GetProperty("monthlyPoints").GetInt32());
        Assert.Equal(1, summary.GetProperty("monthlyRank").GetInt32());
        Assert.Equal(1, summary.GetProperty("allTimeRank").GetInt32());

        using var rankReq = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/rankings/all-time?pageSize=50");
        rankReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
        var rankRes = await _client.SendAsync(rankReq);
        rankRes.EnsureSuccessStatusCode();
        var rankPage = await rankRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(rankPage.GetProperty("totalCount").GetInt32() >= 2);
        var items = rankPage.GetProperty("items");
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
        var first = items[0];
        Assert.Equal(1, first.GetProperty("rank").GetInt32());
        Assert.Equal(TestingSeedConstants.SampleMemberUserId.ToString(), first.GetProperty("userId").GetString());
        Assert.Equal(100, first.GetProperty("totalPoints").GetInt32());
        Assert.Equal("Member Sample", first.GetProperty("userName").GetString());
        var me = rankPage.GetProperty("me");
        Assert.Equal(JsonValueKind.Object, me.ValueKind);
        Assert.Equal(1, me.GetProperty("rank").GetInt32());

        await using (var scope2 = factory.Services.CreateAsyncScope())
        {
            var db = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
            db.LoyaltyPointLedgerEntries.RemoveRange(
                db.LoyaltyPointLedgerEntries.Where(
                    e => e.UserId == TestingSeedConstants.SampleMemberUserId || e.UserId == torcedorUserId));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Invalid_month_returns_empty_ranking_page()
    {
        var member = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/loyalty/rankings/monthly?year=2025&month=13&pageSize=20");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, body.GetProperty("totalCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("me").ValueKind);
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

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
