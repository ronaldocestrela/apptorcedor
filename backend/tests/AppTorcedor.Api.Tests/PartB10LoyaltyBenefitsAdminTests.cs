using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB10LoyaltyBenefitsAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_loyalty_campaigns_requires_fidelidade_visualizar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/loyalty/campaigns");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Loyalty_campaign_publish_and_payment_conciliate_awards_points()
    {
        var token = await LoginAdminAsync();
        Guid campaignId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/loyalty/campaigns"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Campanha P",
                    description = "Teste",
                    rules = new[]
                    {
                        new { trigger = "PaymentPaid", points = 77, sortOrder = 0 },
                    },
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            campaignId = Guid.Parse(body.GetProperty("campaignId").GetString()!);
        }

        using (var pub = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/loyalty/campaigns/{campaignId}/publish"))
        {
            pub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(pub);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        var paymentId = Guid.NewGuid();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var now = DateTimeOffset.UtcNow;
            db.Payments.Add(
                new PaymentRecord
                {
                    Id = paymentId,
                    UserId = TestingSeedConstants.SampleMemberUserId,
                    MembershipId = TestingSeedConstants.SampleMembershipId,
                    Amount = 10m,
                    Status = PaymentChargeStatuses.Pending,
                    DueDate = now,
                    PaidAt = null,
                    PaymentMethod = "Pix",
                    CreatedAt = now,
                    UpdatedAt = now,
                });
            await db.SaveChangesAsync();
        }

        using (var conc = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/payments/{paymentId}/conciliate"))
        {
            conc.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            conc.Content = JsonContent.Create(new { });
            var res = await _client.SendAsync(conc);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var ledger = new HttpRequestMessage(
                   HttpMethod.Get,
                   $"/api/admin/loyalty/users/{TestingSeedConstants.SampleMemberUserId}/ledger?pageSize=50"))
        {
            ledger.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(ledger);
            res.EnsureSuccessStatusCode();
            var page = await res.Content.ReadFromJsonAsync<JsonElement>();
            var items = page.GetProperty("items");
            Assert.Equal(JsonValueKind.Array, items.ValueKind);
            Assert.True(items.GetArrayLength() >= 1);
            var first = items[0];
            Assert.Equal(77, first.GetProperty("points").GetInt32());
            Assert.Equal("Payment", first.GetProperty("sourceType").GetString());
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Payments.Remove(await db.Payments.SingleAsync(p => p.Id == paymentId));
            db.LoyaltyPointLedgerEntries.RemoveRange(db.LoyaltyPointLedgerEntries.Where(e => e.UserId == TestingSeedConstants.SampleMemberUserId));
            db.LoyaltyPointRules.RemoveRange(db.LoyaltyPointRules.Where(r => r.CampaignId == campaignId));
            db.LoyaltyCampaigns.Remove(await db.LoyaltyCampaigns.SingleAsync(c => c.Id == campaignId));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Benefits_partner_offer_redeem_roundtrip()
    {
        var token = await LoginAdminAsync();
        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(new { name = "Parceiro X", description = "d", isActive = true });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            partnerId = Guid.Parse(body.GetProperty("partnerId").GetString()!);
        }

        var now = DateTimeOffset.UtcNow;
        Guid offerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/offers"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    partnerId,
                    title = "Vantagem Y",
                    description = "desc",
                    isActive = true,
                    startAt = now.AddDays(-1),
                    endAt = now.AddDays(30),
                    eligiblePlanIds = (Guid[]?)null,
                    eligibleMembershipStatuses = (string[]?)null,
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            offerId = Guid.Parse(body.GetProperty("offerId").GetString()!);
        }

        using (var redeem = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/benefits/offers/{offerId}/redeem"))
        {
            redeem.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            redeem.Content = JsonContent.Create(
                new { userId = TestingSeedConstants.SampleMemberUserId, notes = "ok" });
            var res = await _client.SendAsync(redeem);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.BenefitRedemptions.RemoveRange(db.BenefitRedemptions.Where(r => r.OfferId == offerId));
            db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
            db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
            await db.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task Benefits_offer_banner_upload_get_delete_roundtrip()
    {
        var token = await LoginAdminAsync();
        Guid partnerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/partners"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(new { name = "Parceiro Banner", description = "d", isActive = true });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            partnerId = Guid.Parse(body.GetProperty("partnerId").GetString()!);
        }

        var now = DateTimeOffset.UtcNow;
        Guid offerId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/benefits/offers"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    partnerId,
                    title = "Oferta com banner",
                    description = "Texto detalhe",
                    isActive = true,
                    startAt = now.AddDays(-1),
                    endAt = now.AddDays(30),
                    eligiblePlanIds = (Guid[]?)null,
                    eligibleMembershipStatuses = (string[]?)null,
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            offerId = Guid.Parse(body.GetProperty("offerId").GetString()!);
        }

        // 1x1 PNG
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8/5+hHgAHggJ/PchI7wAAAABJRU5ErkJggg==");
        using (var upload = new HttpRequestMessage(HttpMethod.Post, $"/api/admin/benefits/offers/{offerId}/banner"))
        {
            upload.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var mp = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(png);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
            mp.Add(fileContent, "file", "banner.png");
            upload.Content = mp;
            var res = await _client.SendAsync(upload);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.GetProperty("bannerUrl").GetString()?.Length > 0);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/benefits/offers/{offerId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var detail = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(string.IsNullOrWhiteSpace(detail.GetProperty("bannerUrl").GetString()));
        }

        using (var del = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/benefits/offers/{offerId}/banner"))
        {
            del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(del);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/benefits/offers/{offerId}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var res = await _client.SendAsync(get);
            res.EnsureSuccessStatusCode();
            var detail = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(detail.GetProperty("bannerUrl").ValueKind == JsonValueKind.Null);
        }

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.BenefitOffers.Remove(await db.BenefitOffers.SingleAsync(o => o.Id == offerId));
            db.BenefitPartners.Remove(await db.BenefitPartners.SingleAsync(p => p.Id == partnerId));
            await db.SaveChangesAsync();
        }
    }

    private async Task<string> LoginTorcedorAsync()
    {
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(
                new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" }),
        };
        var res = await _client.SendAsync(login);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }

    private async Task<string> LoginAdminAsync()
    {
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = "admin@test.local", password = "TestPassword123!" }),
        };
        var res = await _client.SendAsync(login);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }
}
