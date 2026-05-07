using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Payments;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Entities;
using AppTorcedor.Infrastructure.Persistence;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartD4SubscriptionsApiTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Subscribe_requires_auth()
    {
        using var res = await _client.PostAsJsonAsync(
            "/api/subscriptions",
            new { planId = Guid.NewGuid(), paymentMethod = "Pix" },
            JsonOpts);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Subscribe_pix_then_callback_activates_membership_and_idempotent_callback()
    {
        var admin = await LoginAdminAsync();
        var torcedor = await LoginTorcedorAsync();
        Guid planId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D4 PIX",
                    price = 100m,
                    billingCycle = "Monthly",
                    discountPercentage = 10m,
                    isActive = true,
                    isPublished = true,
                    summary = "D4",
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        try
        {
            Guid paymentId;
            Guid membershipId;
            using (var sub = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions"))
            {
                sub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
                sub.Content = JsonContent.Create(new { planId, paymentMethod = "Pix" }, options: JsonOpts);
                var res = await _client.SendAsync(sub);
                res.EnsureSuccessStatusCode();
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                membershipId = body.GetProperty("membershipId").GetGuid();
                paymentId = body.GetProperty("paymentId").GetGuid();
                Assert.Equal("Pix", body.GetProperty("paymentMethod").GetString());
                Assert.Equal(90m, body.GetProperty("amount").GetDecimal());
                Assert.Equal("PendingPayment", body.GetProperty("membershipStatus").GetString());
                var pix = body.GetProperty("pix");
                Assert.Equal(JsonValueKind.Object, pix.ValueKind);
                Assert.Contains("MOCK_PIX", pix.GetProperty("qrCodePayload").GetString(), StringComparison.Ordinal);
            }

            using (var cb = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback"))
            {
                cb.Content = JsonContent.Create(
                    new { paymentId, secret = "test-webhook-secret" },
                    options: JsonOpts);
                var res = await _client.SendAsync(cb);
                Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
            }

            using (var cb2 = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback"))
            {
                cb2.Content = JsonContent.Create(
                    new { paymentId, secret = "test-webhook-secret" },
                    options: JsonOpts);
                var res = await _client.SendAsync(cb2);
                Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var m = await db.Memberships.AsNoTracking().SingleAsync(x => x.Id == membershipId);
                Assert.Equal(MembershipStatus.Ativo, m.Status);
                Assert.NotNull(m.NextDueDate);
                var p = await db.Payments.AsNoTracking().SingleAsync(x => x.Id == paymentId);
                Assert.Equal(PaymentChargeStatuses.Paid, p.Status);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var benefitRows = await db.MembershipPlanBenefits.Where(b => b.PlanId == planId).ToListAsync();
                db.MembershipPlanBenefits.RemoveRange(benefitRows);
                var payments = await db.Payments.Where(p => p.MembershipId != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.Payments.RemoveRange(payments);
                var histories = await db.MembershipHistories.Where(h => h.MembershipId != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.MembershipHistories.RemoveRange(histories);
                var memberships = await db.Memberships.Where(m => m.Id != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.Memberships.RemoveRange(memberships);
                var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId);
                if (plan is not null)
                    db.MembershipPlans.Remove(plan);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Subscribe_card_returns_checkout_url()
    {
        var admin = await LoginAdminAsync();
        var torcedor = await LoginTorcedorAsync();
        Guid planId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D4 Card",
                    price = 20m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = (string?)null,
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        try
        {
            using var sub = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions");
            sub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            sub.Content = JsonContent.Create(new { planId, paymentMethod = "Card" }, options: JsonOpts);
            var res = await _client.SendAsync(sub);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal("Card", body.GetProperty("paymentMethod").GetString());
            var card = body.GetProperty("card");
            Assert.Equal(JsonValueKind.Object, card.ValueKind);
            Assert.Contains("mock-payments.local", card.GetProperty("checkoutUrl").GetString(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var benefitRows = await db.MembershipPlanBenefits.Where(b => b.PlanId == planId).ToListAsync();
                db.MembershipPlanBenefits.RemoveRange(benefitRows);
                var payments = await db.Payments.Where(p => p.MembershipId != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.Payments.RemoveRange(payments);
                var histories = await db.MembershipHistories.Where(h => h.MembershipId != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.MembershipHistories.RemoveRange(histories);
                var memberships = await db.Memberships.Where(m => m.Id != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.Memberships.RemoveRange(memberships);
                var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId);
                if (plan is not null)
                    db.MembershipPlans.Remove(plan);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Recontract_after_payment_confirmation_cancels_legacy_open_charge_and_sweep_keeps_active()
    {
        var testStart = DateTimeOffset.UtcNow;
        var admin = await LoginAdminAsync();
        var member = await LoginMemberAsync();
        var membershipId = TestingSeedConstants.SampleMembershipId;
        var userId = TestingSeedConstants.SampleMemberUserId;
        Guid planId;
        var legacyPaymentId = Guid.Empty;
        var currentPaymentId = Guid.Empty;

        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D4 Recontratacao",
                    price = 79.9m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = "D4 Recontratacao",
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        try
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var membership = await db.Memberships.SingleAsync(m => m.Id == membershipId);
                membership.Status = MembershipStatus.Cancelado;
                membership.NextDueDate = null;
                membership.EndDate = DateTimeOffset.UtcNow.AddDays(-3);

                legacyPaymentId = Guid.NewGuid();
                var now = DateTimeOffset.UtcNow;
                db.Payments.Add(
                    new PaymentRecord
                    {
                        Id = legacyPaymentId,
                        UserId = userId,
                        MembershipId = membershipId,
                        Amount = 59.9m,
                        Status = PaymentChargeStatuses.Overdue,
                        DueDate = now.AddDays(-10),
                        PaidAt = null,
                        PaymentMethod = "Pix",
                        ExternalReference = legacyPaymentId.ToString("N"),
                        ProviderName = "Mock",
                        CreatedAt = now.AddDays(-10),
                        UpdatedAt = now.AddDays(-10),
                        StatusReason = "Cobranca legada em aberto para recontratacao.",
                    });

                await db.SaveChangesAsync();
            }

            using (var sub = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions"))
            {
                sub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", member);
                sub.Content = JsonContent.Create(new { planId, paymentMethod = "Pix" }, options: JsonOpts);
                var res = await _client.SendAsync(sub);
                res.EnsureSuccessStatusCode();
                var body = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(membershipId, body.GetProperty("membershipId").GetGuid());
                currentPaymentId = body.GetProperty("paymentId").GetGuid();
            }

            using (var cb = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback"))
            {
                cb.Content = JsonContent.Create(new { paymentId = currentPaymentId, secret = "test-webhook-secret" }, options: JsonOpts);
                var res = await _client.SendAsync(cb);
                Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var legacy = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == legacyPaymentId);
                Assert.Equal(PaymentChargeStatuses.Cancelled, legacy.Status);

                var current = await db.Payments.AsNoTracking().SingleAsync(p => p.Id == currentPaymentId);
                Assert.Equal(PaymentChargeStatuses.Paid, current.Status);

                var membership = await db.Memberships.AsNoTracking().SingleAsync(m => m.Id == membershipId);
                Assert.Equal(MembershipStatus.Ativo, membership.Status);
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var sweep = scope.ServiceProvider.GetRequiredService<IPaymentDelinquencySweep>();
                await sweep.RunAsync();
            }

            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var membership = await db.Memberships.AsNoTracking().SingleAsync(m => m.Id == membershipId);
                Assert.Equal(MembershipStatus.Ativo, membership.Status);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                if (legacyPaymentId != Guid.Empty)
                {
                    var legacy = await db.Payments.FirstOrDefaultAsync(p => p.Id == legacyPaymentId);
                    if (legacy is not null)
                        db.Payments.Remove(legacy);
                }

                if (currentPaymentId != Guid.Empty)
                {
                    var current = await db.Payments.FirstOrDefaultAsync(p => p.Id == currentPaymentId);
                    if (current is not null)
                        db.Payments.Remove(current);
                }

                var planBenefits = await db.MembershipPlanBenefits.Where(b => b.PlanId == planId).ToListAsync();
                db.MembershipPlanBenefits.RemoveRange(planBenefits);

                var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId);
                if (plan is not null)
                    db.MembershipPlans.Remove(plan);

                var recentHistories = await db.MembershipHistories
                    .Where(h => h.MembershipId == membershipId && h.CreatedAt >= testStart)
                    .ToListAsync();
                db.MembershipHistories.RemoveRange(recentHistories);

                var membership = await db.Memberships.FirstOrDefaultAsync(m => m.Id == membershipId);
                if (membership is not null)
                {
                    membership.Status = MembershipStatus.Ativo;
                    membership.EndDate = null;
                    membership.NextDueDate = DateTimeOffset.UtcNow.AddMonths(1);
                }

                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Callback_with_bad_secret_returns_unauthorized()
    {
        using var cb = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback");
        cb.Content = JsonContent.Create(
            new { paymentId = Guid.NewGuid(), secret = "wrong" },
            options: JsonOpts);
        var res = await _client.SendAsync(cb);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Second_subscribe_while_pending_payment_returns_conflict()
    {
        var admin = await LoginAdminAsync();
        var torcedor = await LoginTorcedorAsync();
        Guid planId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D4 Pending",
                    price = 10m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = (string?)null,
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        try
        {
            using (var sub = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions"))
            {
                sub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
                sub.Content = JsonContent.Create(new { planId, paymentMethod = "Pix" }, options: JsonOpts);
                var res = await _client.SendAsync(sub);
                res.EnsureSuccessStatusCode();
            }

            using (var sub2 = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions"))
            {
                sub2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
                sub2.Content = JsonContent.Create(new { planId, paymentMethod = "Pix" }, options: JsonOpts);
                var res = await _client.SendAsync(sub2);
                Assert.Equal(HttpStatusCode.Conflict, res.StatusCode);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var benefitRows = await db.MembershipPlanBenefits.Where(b => b.PlanId == planId).ToListAsync();
                db.MembershipPlanBenefits.RemoveRange(benefitRows);
                var payments = await db.Payments.Where(p => p.MembershipId != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.Payments.RemoveRange(payments);
                var histories = await db.MembershipHistories.Where(h => h.MembershipId != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.MembershipHistories.RemoveRange(histories);
                var memberships = await db.Memberships.Where(m => m.Id != TestingSeedConstants.SampleMembershipId).ToListAsync();
                db.Memberships.RemoveRange(memberships);
                var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId);
                if (plan is not null)
                    db.MembershipPlans.Remove(plan);
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
