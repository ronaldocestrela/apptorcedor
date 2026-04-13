using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

/// <summary>Usa factory com seed LGPD mínimo para permitir cadastro isolado (sem membership) sem depender da ordem dos testes.</summary>
public sealed class PartD5SubscriptionSummaryApiTests(AppWebApplicationFactoryWithLegalSeed factory)
    : IClassFixture<AppWebApplicationFactoryWithLegalSeed>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Subscription_summary_requires_auth()
    {
        var res = await _client.GetAsync("/api/account/subscription");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Freshly_registered_user_has_no_membership_row()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        req.EnsureSuccessStatusCode();
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"d5-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "D5 Sem Plano",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11988887777",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.Contains(SystemRoles.Torcedor, auth!.Roles);

        using var sum = new HttpRequestMessage(HttpMethod.Get, "/api/account/subscription");
        sum.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
        var res = await _client.SendAsync(sum);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.False(body.GetProperty("hasMembership").GetBoolean());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("membershipId").ValueKind);
    }

    [Fact]
    public async Task Sample_member_gets_membership_and_digital_card_summary()
    {
        var token = await LoginMemberAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/account/subscription");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(body.GetProperty("hasMembership").GetBoolean());
        Assert.Equal(
            TestingSeedConstants.SampleMembershipId.ToString(),
            body.GetProperty("membershipId").GetString());
        Assert.Equal("NaoAssociado", body.GetProperty("membershipStatus").GetString());
        Assert.True(body.TryGetProperty("digitalCard", out var dc) && dc.ValueKind == JsonValueKind.Object);
        Assert.Equal(nameof(MyDigitalCardViewState.NotAssociated), dc.GetProperty("state").GetString());
    }

    [Fact]
    public async Task After_subscribe_and_callback_summary_shows_active_and_last_paid_payment()
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
                    name = "Plano D5 resumo",
                    price = 80m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = "D5",
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        Guid paymentId;
        using (var sub = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions"))
        {
            sub.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            sub.Content = JsonContent.Create(new { planId, paymentMethod = "Pix" }, options: JsonOpts);
            var res = await _client.SendAsync(sub);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            paymentId = body.GetProperty("paymentId").GetGuid();
        }

        using (var cb = new HttpRequestMessage(HttpMethod.Post, "/api/subscriptions/payments/callback"))
        {
            cb.Content = JsonContent.Create(new { paymentId, secret = "test-webhook-secret" }, options: JsonOpts);
            var res = await _client.SendAsync(cb);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        using var sumReq = new HttpRequestMessage(HttpMethod.Get, "/api/account/subscription");
        sumReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
        var sumRes = await _client.SendAsync(sumReq);
        sumRes.EnsureSuccessStatusCode();
        var sum = await sumRes.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.True(sum.GetProperty("hasMembership").GetBoolean());
        Assert.Equal("Ativo", sum.GetProperty("membershipStatus").GetString());
        Assert.True(sum.TryGetProperty("lastPayment", out var lp) && lp.ValueKind == JsonValueKind.Object);
        Assert.Equal(paymentId.ToString(), lp.GetProperty("paymentId").GetString());
        Assert.Equal("Paid", lp.GetProperty("status").GetString());
        Assert.Equal(80m, lp.GetProperty("amount").GetDecimal());
        Assert.True(sum.TryGetProperty("plan", out var pl) && pl.ValueKind == JsonValueKind.Object);
        Assert.Equal("Plano D5 resumo", pl.GetProperty("name").GetString());
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

    private async Task<string> LoginTorcedorAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
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

    private sealed record RequirementsDto(Guid TermsOfUseVersionId, Guid PrivacyPolicyVersionId, string TermsTitle, string PrivacyTitle);

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
