using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AppTorcedor.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartD7CancelSubscriptionApiTests(AppWebApplicationFactoryWithLegalSeed factory)
    : IClassFixture<AppWebApplicationFactoryWithLegalSeed>
{
    private readonly AppWebApplicationFactoryWithLegalSeed _factory = factory;
    private readonly HttpClient _client = factory.CreateClient();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    [Fact]
    public async Task Cancel_subscription_requires_auth()
    {
        var res = await _client.DeleteAsync("/api/account/subscription");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Cancel_subscription_returns_not_found_when_user_has_no_membership_row()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        req.EnsureSuccessStatusCode();
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"d7-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "D7 Sem Plano",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11988887777",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        using var del = new HttpRequestMessage(HttpMethod.Delete, "/api/account/subscription");
        del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        var res = await _client.SendAsync(del);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Cancel_subscription_immediate_within_cooling_off()
    {
        var (token, _) = await CreateActiveSubscriberAsync();
        using var del = new HttpRequestMessage(HttpMethod.Delete, "/api/account/subscription");
        del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(del);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Cancelado", body.GetProperty("membershipStatus").GetString());
        Assert.Equal("Immediate", body.GetProperty("mode").GetString());

        using var del2 = new HttpRequestMessage(HttpMethod.Delete, "/api/account/subscription");
        del2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res2 = await _client.SendAsync(del2);
        Assert.Equal(HttpStatusCode.Conflict, res2.StatusCode);
    }

    [Fact]
    public async Task Cancel_subscription_scheduled_when_outside_cooling_off()
    {
        var (token, email) = await CreateActiveSubscriberAsync();
        await ShiftMembershipStartDateAsync(email, daysAgo: 30);

        using var del = new HttpRequestMessage(HttpMethod.Delete, "/api/account/subscription");
        del.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(del);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        Assert.Equal("Ativo", body.GetProperty("membershipStatus").GetString());
        Assert.Equal("ScheduledEndOfCycle", body.GetProperty("mode").GetString());
        Assert.NotEqual(JsonValueKind.Null, body.GetProperty("accessValidUntilUtc").ValueKind);
    }

    private async Task ShiftMembershipStartDateAsync(string userEmail, int daysAgo)
    {
        using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = await db.Users.AsNoTracking().FirstAsync(u => u.Email == userEmail);
        var m = await db.Memberships.FirstAsync(x => x.UserId == user.Id);
        m.StartDate = DateTimeOffset.UtcNow.AddDays(-daysAgo);
        await db.SaveChangesAsync();
    }

    private async Task<(string Token, string Email)> CreateActiveSubscriberAsync()
    {
        var admin = await LoginAdminAsync();

        Guid planId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D7",
                    price = 90m,
                    billingCycle = "Monthly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = true,
                    summary = "D7",
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            planId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        var req = await _client.GetAsync("/api/account/register/requirements");
        req.EnsureSuccessStatusCode();
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"d7-sub-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "D7 Subscriber",
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
            res.EnsureSuccessStatusCode();
        }

        return (token, email);
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
