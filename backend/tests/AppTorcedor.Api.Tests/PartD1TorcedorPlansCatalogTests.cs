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

public sealed class PartD1TorcedorPlansCatalogTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Plans_catalog_requires_auth()
    {
        var res = await _client.GetAsync("/api/plans");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Plans_catalog_lists_only_published_active_plans_with_benefits()
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
                    name = "Plano D1 Catálogo",
                    price = 99.9m,
                    billingCycle = "Monthly",
                    discountPercentage = 5m,
                    isActive = true,
                    isPublished = true,
                    summary = "Resumo D1",
                    rulesNotes = (string?)null,
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

        Guid draftId;
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", admin);
            post.Content = JsonContent.Create(
                new
                {
                    name = "Plano D1 Rascunho",
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
            using (var get = new HttpRequestMessage(HttpMethod.Get, "/api/plans"))
            {
                get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", torcedor);
                var res = await _client.SendAsync(get);
                res.EnsureSuccessStatusCode();
                var page = await res.Content.ReadFromJsonAsync<JsonElement>();
                var items = page.GetProperty("items");
                var foundPublished = false;
                var foundDraft = false;
                foreach (var el in items.EnumerateArray())
                {
                    var id = el.GetProperty("planId").GetGuid();
                    if (id == publishedId)
                    {
                        foundPublished = true;
                        Assert.Equal("Plano D1 Catálogo", el.GetProperty("name").GetString());
                        Assert.Equal(99.9m, el.GetProperty("price").GetDecimal());
                        Assert.Equal("Monthly", el.GetProperty("billingCycle").GetString());
                        Assert.Equal(5m, el.GetProperty("discountPercentage").GetDecimal());
                        Assert.Equal("Resumo D1", el.GetProperty("summary").GetString());
                        var benefits = el.GetProperty("benefits");
                        Assert.Equal(2, benefits.GetArrayLength());
                        Assert.Equal("Benefício B", benefits[0].GetProperty("title").GetString());
                        Assert.Equal("Benefício A", benefits[1].GetProperty("title").GetString());
                    }

                    if (id == draftId)
                        foundDraft = true;
                }

                Assert.True(foundPublished);
                Assert.False(foundDraft);
            }
        }
        finally
        {
            await using (var scope = factory.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                foreach (var planId in new[] { publishedId, draftId })
                {
                    var benefitRows = await db.MembershipPlanBenefits.Where(b => b.PlanId == planId).ToListAsync();
                    db.MembershipPlanBenefits.RemoveRange(benefitRows);
                    var plan = await db.MembershipPlans.FirstOrDefaultAsync(p => p.Id == planId);
                    if (plan is not null)
                        db.MembershipPlans.Remove(plan);
                }

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
}
