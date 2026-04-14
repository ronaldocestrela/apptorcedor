using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartD6ChangePlanApiTests(AppWebApplicationFactoryWithLegalSeed factory)
    : IClassFixture<AppWebApplicationFactoryWithLegalSeed>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Change_plan_requires_auth()
    {
        var res = await _client.PutAsJsonAsync(
            "/api/account/subscription/plan",
            new { planId = Guid.NewGuid(), paymentMethod = "Pix" },
            JsonOpts);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Change_plan_returns_not_found_when_user_has_no_membership()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        req.EnsureSuccessStatusCode();
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"d6-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "D6 Sem Plano",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11988887777",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/subscription/plan");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        put.Content = JsonContent.Create(new { planId = Guid.NewGuid(), paymentMethod = "Pix" }, options: JsonOpts);
        var res = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Change_plan_returns_bad_request_for_invalid_plan()
    {
        var (token, _, _) = await CreateActiveSubscriberWithPlansAsync();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/subscription/plan");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(
            new { planId = Guid.NewGuid(), paymentMethod = "Pix" },
            options: JsonOpts);
        var res = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Change_plan_returns_bad_request_for_same_plan()
    {
        var (token, planA, _) = await CreateActiveSubscriberWithPlansAsync();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/subscription/plan");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(new { planId = planA, paymentMethod = "Pix" }, options: JsonOpts);
        var res = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Change_plan_pix_returns_proration_checkout()
    {
        var (token, _, planB) = await CreateActiveSubscriberWithPlansAsync();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/subscription/plan");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(new { planId = planB, paymentMethod = "Pix" }, options: JsonOpts);
        var res = await _client.SendAsync(put);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Ativo", body.GetProperty("membershipStatus").GetString());
        Assert.True(body.GetProperty("prorationAmount").GetDecimal() > 0);
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("paymentId").ValueKind);
        Assert.True(body.TryGetProperty("pix", out var pix) && pix.ValueKind == JsonValueKind.Object);
        Assert.False(string.IsNullOrEmpty(pix.GetProperty("qrCodePayload").GetString()));
    }

    [Fact]
    public async Task Change_plan_card_returns_checkout_url()
    {
        var (token, _, planB) = await CreateActiveSubscriberWithPlansAsync();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/subscription/plan");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(new { planId = planB, paymentMethod = "Card" }, options: JsonOpts);
        var res = await _client.SendAsync(put);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.TryGetProperty("card", out var card) && card.ValueKind == JsonValueKind.Object);
        Assert.Contains("mock-payments.local", card.GetProperty("checkoutUrl").GetString());
    }

    [Fact]
    public async Task Proration_payment_callback_keeps_membership_active()
    {
        var (token, _, planB) = await CreateActiveSubscriberWithPlansAsync();
        Guid paymentId;
        using (var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/subscription/plan"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            put.Content = JsonContent.Create(new { planId = planB, paymentMethod = "Pix" }, options: JsonOpts);
            var res = await _client.SendAsync(put);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
            paymentId = body.GetProperty("paymentId").GetGuid();
        }

        using (var cb = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback"))
        {
            cb.Content = JsonContent.Create(new { paymentId, secret = "test-webhook-secret" }, options: JsonOpts);
            var res = await _client.SendAsync(cb);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using var sumReq = new HttpRequestMessage(HttpMethod.Get, "/api/account/subscription");
        sumReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var sumRes = await _client.SendAsync(sumReq);
        sumRes.EnsureSuccessStatusCode();
        var sum = await sumRes.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Ativo", sum.GetProperty("membershipStatus").GetString());
        Assert.True(sum.TryGetProperty("plan", out var pl) && pl.ValueKind == JsonValueKind.Object);
        Assert.Equal(planB.ToString(), pl.GetProperty("planId").GetString());
    }

    private async Task<(string Token, Guid PlanA, Guid PlanB)> CreateActiveSubscriberWithPlansAsync()
    {
        var admin = await LoginAdminAsync();

        Guid planA;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D6 base",
                    price = 80m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = "D6A",
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planA = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        Guid planB;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D6 upgrade",
                    price = 160m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = "D6B",
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planB = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        var req = await _client.GetAsync("/api/account/register/requirements");
        req.EnsureSuccessStatusCode();
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"d6-sub-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "D6 Subscriber",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11988887777",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        var token = auth!.AccessToken;

        Guid paymentId;
        using (var sub = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions"))
        {
            sub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            sub.Content = JsonContent.Create(new { planId = planA, paymentMethod = "Pix" }, options: JsonOpts);
            var res = await _client.SendAsync(sub);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            paymentId = body.GetProperty("paymentId").GetGuid();
        }

        using (var cb = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback"))
        {
            cb.Content = JsonContent.Create(new { paymentId, secret = "test-webhook-secret" }, options: JsonOpts);
            var res = await _client.SendAsync(cb);
            res.EnsureSuccessStatusCode();
        }

        return (token, planA, planB);
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

    private sealed record RequirementsDto(Guid TermsOfUseVersionId, Guid PrivacyPolicyVersionId, string TermsTitle, string PrivacyTitle);

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
