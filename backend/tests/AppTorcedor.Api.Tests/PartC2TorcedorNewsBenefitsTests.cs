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

public sealed class PartC2TorcedorNewsBenefitsTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task News_feed_allows_anonymous()
    {
        var res = await _client.GetAsync("/api/news");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Benefits_eligible_requires_auth()
    {
        var res = await _client.GetAsync("/api/benefits/eligible");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Published_news_visible_in_feed_and_detail_draft_is_not()
    {
        var admin = await LoginAdminAsync();
        var torcedor = await LoginTorcedorAsync();

        Guid draftId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/news"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { title = "Rascunho C2", summary = "S", content = "C" });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            draftId = Guid.Parse(body.GetProperty("newsId").GetString()!);
        }

        using (var feed = new HttpRequestMessage(HttpMethod.Get, "/api/news?pageSize=50"))
        {
            feed.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            var res = await _client.SendAsync(feed);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            var items = page.GetProperty("items");
            foreach (var el in items.EnumerateArray())
            {
                Assert.NotEqual(draftId.ToString(), el.GetProperty("newsId").GetString());
            }
        }

        using (var detailDraft = new HttpRequestMessage(HttpMethod.Get, $"/api/news/{draftId}"))
        {
            detailDraft.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            var res = await _client.SendAsync(detailDraft);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }

        using (var pub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{draftId}/publish"))
        {
            pub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            (await _client.SendAsync(pub)).EnsureSuccessStatusCode();
        }

        using (var feed2 = new HttpRequestMessage(HttpMethod.Get, "/api/news?search=C2"))
        {
            feed2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            var res = await _client.SendAsync(feed2);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            var items = page.GetProperty("items");
            Assert.True(items.GetArrayLength() >= 1);
            var found = false;
            foreach (var el in items.EnumerateArray())
            {
                if (el.GetProperty("newsId").GetString() == draftId.ToString())
                    found = true;
            }
            Assert.True(found);
        }

        using (var detail = new HttpRequestMessage(HttpMethod.Get, $"/api/news/{draftId}"))
        {
            detail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            var res = await _client.SendAsync(detail);
            res.EnsureSuccessStatusCode();
            var d = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Rascunho C2", d.GetProperty("title").GetString());
        }

        using (var unpub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/news/{draftId}/unpublish"))
        {
            unpub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            (await _client.SendAsync(unpub)).EnsureSuccessStatusCode();
        }

        using (var detailGone = new HttpRequestMessage(HttpMethod.Get, $"/api/news/{draftId}"))
        {
            detailGone.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            var res = await _client.SendAsync(detailGone);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
        }
    }

    [Fact]
    public async Task Eligible_benefits_open_offer_lists_for_torcedor_member_without_plan_restriction()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "Parceiro C2", description = "d", isActive = true });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            partnerId = Guid.Parse(body.GetProperty("partnerId").GetString()!);
        }

        var now = DateTimeOffset.UtcNow;
        Guid offerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/offers"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    partnerId,
                    title = "Oferta C2",
                    description = "desc",
                    isActive = true,
                    startAt = now.AddDays(-1),
                    endAt = now.AddDays(30),
                    eligiblePlanIds = (Guid[]?)null,
                    eligibleMembershipStatuses = (string[]?)null,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            offerId = Guid.Parse(body.GetProperty("offerId").GetString()!);
        }

        using (var eligible = new HttpRequestMessage(HttpMethod.Get, "/api/benefits/eligible?pageSize=50"))
        {
            eligible.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
            var res = await _client.SendAsync(eligible);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            var items = page.GetProperty("items");
            var found = false;
            foreach (var el in items.EnumerateArray())
            {
                if (el.GetProperty("offerId").GetString() == offerId.ToString())
                    found = true;
            }
            Assert.True(found);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
            db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Eligible_benefits_respects_plan_restriction()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid planId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano C2",
                    price = 10m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "Parceiro C2b", description = "d", isActive = true });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            partnerId = Guid.Parse(body.GetProperty("partnerId").GetString()!);
        }

        var now = DateTimeOffset.UtcNow;
        Guid offerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/offers"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    partnerId,
                    title = "Só plano C2",
                    description = "x",
                    isActive = true,
                    startAt = now.AddDays(-1),
                    endAt = now.AddDays(30),
                    eligiblePlanIds = new[] { planId },
                    eligibleMembershipStatuses = (string[]?)null,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            offerId = Guid.Parse(body.GetProperty("offerId").GetString()!);
        }

        using (var eligible = new HttpRequestMessage(HttpMethod.Get, "/api/benefits/eligible?pageSize=50"))
        {
            eligible.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
            var res = await _client.SendAsync(eligible);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            var items = page.GetProperty("items");
            foreach (var el in items.EnumerateArray())
                Assert.NotEqual(offerId.ToString(), el.GetProperty("offerId").GetString());
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var m = await db.Memberships.FirstAsync(x => x.Id == TestingSeedConstants.SampleMembershipId);
            m.PlanId = planId;
            await db.SaveChangesAsync();
        }

        try
        {
            using (var eligible2 = new HttpRequestMessage(HttpMethod.Get, "/api/benefits/eligible?pageSize=50"))
            {
                eligible2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(eligible2);
                res.EnsureSuccessStatusCode();
                var page = await res.Content.ReadFromJsonAsync<JsonElement>();
                var items = page.GetProperty("items");
                var found = false;
                foreach (var el in items.EnumerateArray())
                {
                    if (el.GetProperty("offerId").GetString() == offerId.ToString())
                        found = true;
                }
                Assert.True(found);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var m = await db.Memberships.FirstAsync(x => x.Id == TestingSeedConstants.SampleMembershipId);
                m.PlanId = null;
                db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
                db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
                db.MembershipPlans.Remove(await db.MembershipPlans.SingleAsync(p => p.Id == planId));
                await db.SaveChangesAsync();
            }
        }
    }

    private async Task<string> LoginAdminAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        return tokens.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> LoginTorcedorAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        return tokens.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> LoginMemberAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.MemberEmail, password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<JsonElement>();
        return tokens.GetProperty("accessToken").GetString()!;
    }
}
