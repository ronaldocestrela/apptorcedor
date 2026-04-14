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

public sealed class PartD2TorcedorPlansDetailTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Plan_detail_requires_auth()
    {
        var id = Guid.NewGuid();
        var res = await _client.GetAsync($"/api/plans/{id}");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Plan_detail_returns_published_active_plan_with_rules_and_benefits()
    {
        var admin = await LoginAdminAsync();
        var torcedor = await LoginTorcedorAsync();

        Guid publishedId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D2 Detalhe",
                    price = 100m,
                    billingCycle = "Monthly",
                    discountPercentage = 10m,
                    isActive = true,
                    isPublished = true,
                    summary = "Resumo D2",
                    rulesNotes = "Regras do plano D2",
                    benefits = new object[]
                    {
                        new { sortOrder = 1, title = "Benefício A", description = "Desc A" },
                        new { sortOrder = 0, title = "Benefício B", description = "Desc B" },
                    },
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            publishedId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        try
        {
            using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/plans/{publishedId}"))
            {
                get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
                var res = await _client.SendAsync(get);
                res.EnsureSuccessStatusCode();
                var el = await res.Content.ReadFromJsonAsync<JsonElement>();
                Assert.Equal(publishedId, el.GetProperty("planId").GetGuid());
                Assert.Equal("Plano D2 Detalhe", el.GetProperty("name").GetString());
                Assert.Equal(100m, el.GetProperty("price").GetDecimal());
                Assert.Equal("Monthly", el.GetProperty("billingCycle").GetString());
                Assert.Equal(10m, el.GetProperty("discountPercentage").GetDecimal());
                Assert.Equal("Resumo D2", el.GetProperty("summary").GetString());
                Assert.Equal("Regras do plano D2", el.GetProperty("rulesNotes").GetString());
                var benefits = el.GetProperty("benefits");
                Assert.Equal(2, benefits.GetArrayLength());
                Assert.Equal("Benefício B", benefits[0].GetProperty("title").GetString());
                Assert.Equal(0, benefits[0].GetProperty("sortOrder").GetInt32());
                Assert.Equal("Benefício A", benefits[1].GetProperty("title").GetString());
                Assert.Equal(1, benefits[1].GetProperty("sortOrder").GetInt32());
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var benefitRows = await db.MembershipPlanBenefits.Where(b => b.PlanId == publishedId).ToListAsync();
                db.MembershipPlanBenefits.RemoveRange(benefitRows);
                var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == publishedId);
                if (plan is not null)
                    db.MembershipPlans.Remove(plan);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Plan_detail_returns_not_found_for_draft_plan()
    {
        var admin = await LoginAdminAsync();
        var torcedor = await LoginTorcedorAsync();

        Guid draftId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D2 Rascunho",
                    price = 1m,
                    billingCycle = "Yearly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = false,
                    summary = (string?)null,
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var res = await _client.SendAsync(post);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            draftId = Guid.Parse(body.GetProperty("planId").GetString()!);
        }

        try
        {
            using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/plans/{draftId}"))
            {
                get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
                var res = await _client.SendAsync(get);
                Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var benefitRows = await db.MembershipPlanBenefits.Where(b => b.PlanId == draftId).ToListAsync();
                db.MembershipPlanBenefits.RemoveRange(benefitRows);
                var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == draftId);
                if (plan is not null)
                    db.MembershipPlans.Remove(plan);
                await db.SaveChangesAsync();
            }
        }
    }

    [Fact]
    public async Task Plan_detail_returns_not_found_for_unknown_id()
    {
        var torcedor = await LoginTorcedorAsync();
        var id = Guid.NewGuid();
        using (var get = new HttpRequestMessage(HttpMethod.Get, $"/api/plans/{id}"))
        {
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
            var res = await _client.SendAsync(get);
            Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
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
}
