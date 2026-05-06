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

public sealed class TorcedorBenefitRedemptionApiTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Benefits_offer_detail_requires_auth()
    {
        var res = await _client.GetAsync($"/api/benefits/offers/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Benefits_redeem_requires_auth()
    {
        var res = await _client.PostAsync($"/api/benefits/offers/{Guid.NewGuid()}/redeem", null);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Detail_and_self_redeem_flow_for_open_offer()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "Parceiro Redeem API", description = "d", isActive = true });
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
                    title = "Oferta Redeem API",
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

        try
        {
            using (var detail = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail);
                res.EnsureSuccessStatusCode();
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("Oferta Redeem API", d.GetProperty("title").GetString());
                Assert.False(d.GetProperty("alreadyRedeemed").GetBoolean());
            }

            using (var redeem = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem"))
            {
                redeem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(redeem);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(Guid.TryParse(body.GetProperty("redemptionId").GetString(), out _));
            }

            using (var detail2 = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail2);
                res.EnsureSuccessStatusCode();
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(d.GetProperty("alreadyRedeemed").GetBoolean());
                Assert.NotNull(d.GetProperty("redemptionDateUtc").GetString());
            }

            using (var redeem2 = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem"))
            {
                redeem2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(redeem2);
                Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                foreach (var r in db.BenefitRedemptions.Where(x => x.OfferId == offerId))
                    db.BenefitRedemptions.Remove(r);
                db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
                db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Shirt_offer_pending_then_approve_flow()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "Parceiro Camisa API", description = "d", isActive = true });
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
                    title = "Camisa API",
                    description = "desc",
                    isActive = true,
                    startAt = now.AddDays(-1),
                    endAt = now.AddDays(30),
                    eligiblePlanIds = (Guid[]?)null,
                    eligibleMembershipStatuses = (string[]?)null,
                    isShirtCustomizationOffer = true,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            offerId = Guid.Parse(body.GetProperty("offerId").GetString()!);
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/benefits/offers/{offerId}/shirt-catalog"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            put.Content = JsonContent.Create(new { sizes = new[] { "M", "G" }, models = new[] { "Home" } });
            var res = await _client.SendAsync(put);
            res.EnsureSuccessStatusCode();
        }

        Guid redemptionId;
        try
        {
            using (var redeem = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem"))
            {
                redeem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                redeem.Content = JsonContent.Create(
                    new
                    {
                        shirtSize = "M",
                        shirtModel = "Home",
                        shirtNumber = "10",
                        shirtDisplayName = "Fulano",
                        deliveryCep = "01310100",
                        deliveryNeighborhood = "Centro",
                        deliveryStreet = "Rua A",
                        deliveryNumber = "10",
                        deliveryCity = "São Paulo",
                        deliveryState = "SP",
                    });
                var res = await _client.SendAsync(redeem);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                redemptionId = Guid.Parse(body.GetProperty("redemptionId").GetString()!);
            }

            using (var detail = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail);
                res.EnsureSuccessStatusCode();
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(d.GetProperty("isShirtCustomizationOffer").GetBoolean());
                Assert.Equal("pending", d.GetProperty("redemptionWorkflowStatus").GetString());
                Assert.False(d.GetProperty("alreadyRedeemed").GetBoolean());
            }

            using (var approve = new HttpRequestMessage(
                       HttpMethod.Post,
                       $"/api/admin/benefits/redemptions/{redemptionId}/approve"))
            {
                approve.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
                var res = await _client.SendAsync(approve);
                res.EnsureSuccessStatusCode();
            }

            using (var detail2 = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail2);
                res.EnsureSuccessStatusCode();
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("approved", d.GetProperty("redemptionWorkflowStatus").GetString());
                Assert.True(d.GetProperty("alreadyRedeemed").GetBoolean());
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                foreach (var r in db.BenefitRedemptions.Where(x => x.OfferId == offerId))
                    db.BenefitRedemptions.Remove(r);
                foreach (var c in db.BenefitShirtCatalogOptions.Where(x => x.OfferId == offerId))
                    db.BenefitShirtCatalogOptions.Remove(c);
                db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
                db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Shirt_offer_redeem_validation_when_delivery_missing()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "P End", description = "d", isActive = true });
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
                    title = "Camisa sem endereço",
                    description = "d",
                    isActive = true,
                    startAt = now.AddDays(-1),
                    endAt = now.AddDays(30),
                    eligiblePlanIds = (Guid[]?)null,
                    eligibleMembershipStatuses = (string[]?)null,
                    isShirtCustomizationOffer = true,
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            offerId = Guid.Parse(body.GetProperty("offerId").GetString()!);
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/benefits/offers/{offerId}/shirt-catalog"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            put.Content = JsonContent.Create(new { sizes = new[] { "M" }, models = new[] { "Home" } });
            var res = await _client.SendAsync(put);
            res.EnsureSuccessStatusCode();
        }

        try
        {
            using var redeem = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem");
            redeem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
            redeem.Content = JsonContent.Create(
                new { shirtSize = "M", shirtModel = "Home", shirtNumber = "7", shirtDisplayName = "A" });
            var res = await _client.SendAsync(redeem);
            Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                foreach (var r in db.BenefitRedemptions.Where(x => x.OfferId == offerId))
                    db.BenefitRedemptions.Remove(r);
                foreach (var c in db.BenefitShirtCatalogOptions.Where(x => x.OfferId == offerId))
                    db.BenefitShirtCatalogOptions.Remove(c);
                db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
                db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Redeem_returns_not_found_for_unknown_offer()
    {
        var memberToken = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{Guid.NewGuid()}/redeem");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
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
