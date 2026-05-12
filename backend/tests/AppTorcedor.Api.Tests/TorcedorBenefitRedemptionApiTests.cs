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
    public async Task Shirt_offer_pending_then_approve_flow_pickup()
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
                    title = "Camisa API Pickup",
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
                        shippingMethod = "pickup",
                    });
                var res = await _client.SendAsync(redeem);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                redemptionId = Guid.Parse(body.GetProperty("redemptionId").GetString()!);
                Assert.Null(body.GetProperty("checkoutUrl").GetString());
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
    public async Task Shirt_offer_carrier_redeem_returns_checkout_url_and_awaiting_shipping_payment()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "Parceiro Camisa Carr", description = "d", isActive = true });
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
                    title = "Camisa API Carrier",
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
                        shippingMethod = "carrier",
                        shippingCarrierId = 2,
                        shippingCarrierName = "Correios",
                        shippingServiceName = "SEDEX",
                        shippingPrice = 12.68m,
                        shippingDeliveryDays = 2,
                    });
                var res = await _client.SendAsync(redeem);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.True(Guid.TryParse(body.GetProperty("redemptionId").GetString(), out _));
                var url = body.GetProperty("checkoutUrl").GetString();
                Assert.False(string.IsNullOrWhiteSpace(url));
                Assert.StartsWith("https://mock-payments.local/checkout/", url, StringComparison.Ordinal);
            }

            using (var detail = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail);
                res.EnsureSuccessStatusCode();
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("awaiting_shipping_payment", d.GetProperty("redemptionWorkflowStatus").GetString());
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var payIds = await db.BenefitRedemptions.AsNoTracking()
                    .Where(x => x.OfferId == offerId)
                    .Select(x => x.ShippingPaymentId)
                    .Where(x => x != null)
                    .ToListAsync();
                foreach (var r in db.BenefitRedemptions.Where(x => x.OfferId == offerId).ToList())
                    db.BenefitRedemptions.Remove(r);
                foreach (var pid in payIds)
                {
                    if (pid is not { } id)
                        continue;
                    var p = await db.Payments.FirstOrDefaultAsync(x => x.Id == id);
                    if (p is not null)
                        db.Payments.Remove(p);
                }
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
    public async Task Benefits_shipping_options_requires_auth()
    {
        var res = await _client.GetAsync("/api/benefits/shipping-options?cep=01310100");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Benefits_shipping_options_returns_array_for_member()
    {
        var memberToken = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/benefits/shipping-options?cep=01310100");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var arr = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, arr.ValueKind);
    }

    [Fact]
    public async Task Shirt_offer_pickup_member_redeem_returns_created()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "P Pickup API", description = "d", isActive = true });
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
                    title = "Camisa Pickup",
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
            using (var redeem = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem"))
            {
                redeem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                redeem.Content = JsonContent.Create(
                    new
                    {
                        shirtSize = "M",
                        shirtModel = "Home",
                        shirtNumber = "9",
                        shirtDisplayName = "Teste",
                        shippingMethod = "pickup",
                    });
                var res = await _client.SendAsync(redeem);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var row = await db.BenefitRedemptions.AsNoTracking().FirstOrDefaultAsync(x => x.OfferId == offerId);
                Assert.NotNull(row);
                Assert.Equal("pickup", row.ShippingMethod);
                Assert.Null(row.DeliveryCep);
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
    public async Task Redeem_returns_not_found_for_unknown_offer()
    {
        var memberToken = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{Guid.NewGuid()}/redeem");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Cancel_redemption_requires_auth()
    {
        var res = await _client.DeleteAsync($"/api/benefits/offers/{Guid.NewGuid()}/redemption");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Cancel_redemption_returns_not_found_when_no_active_redemption()
    {
        var memberToken = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Delete, $"/api/benefits/offers/{Guid.NewGuid()}/redemption");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Cancel_and_reredeem_flow_puts_new_redemption_in_pending()
    {
        var admin = await LoginAdminAsync();
        var memberToken = await LoginMemberAsync();

        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(new { name = "Parceiro Cancel API", description = "d", isActive = true });
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
                    title = "Oferta Cancel API",
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
            // Redeem
            using (var redeem = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem"))
            {
                redeem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(redeem);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            }

            // Detail shows approved
            using (var detail = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail);
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("approved", d.GetProperty("redemptionWorkflowStatus").GetString());
            }

            // Cancel
            using (var cancel = new HttpRequestMessage(HttpMethod.Delete, $"/api/benefits/offers/{offerId}/redemption"))
            {
                cancel.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(cancel);
                Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
            }

            // Detail shows cancelled and requiresApprovalForNextRedemption = true
            using (var detail2 = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail2);
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("cancelled", d.GetProperty("redemptionWorkflowStatus").GetString());
                Assert.True(d.GetProperty("requiresApprovalForNextRedemption").GetBoolean());
                Assert.False(d.GetProperty("alreadyRedeemed").GetBoolean());
            }

            // Re-redeem → must be pending
            using (var redeem2 = new HttpRequestMessage(HttpMethod.Post, $"/api/benefits/offers/{offerId}/redeem"))
            {
                redeem2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(redeem2);
                Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            }

            using (var detail3 = new HttpRequestMessage(HttpMethod.Get, $"/api/benefits/offers/{offerId}"))
            {
                detail3.Headers.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);
                var res = await _client.SendAsync(detail3);
                var d = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal("pending", d.GetProperty("redemptionWorkflowStatus").GetString());
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
