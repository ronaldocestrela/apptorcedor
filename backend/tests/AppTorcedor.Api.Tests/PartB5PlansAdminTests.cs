using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartB5PlansAdminTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task List_plans_requires_planos_visualizar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/plans");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task List_plans_returns_ok_for_admin()
    {
        var token = await LoginAdminAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/plans?pageSize=50");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.TryGetProperty("items", out var items));
        Assert.Equal(JsonValueKind.Array, items.ValueKind);
    }

    [Fact]
    public async Task Create_plan_requires_planos_criar()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = JsonContent.Create(MinimalPlanBody());
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Create_get_and_update_plan_roundtrip()
    {
        var token = await LoginAdminAsync();
        var name = $"Plan-{Guid.NewGuid():N}".Substring(0, 12);
        using (var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans"))
        {
            post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            post.Content = JsonContent.Create(
                new
                {
                    name,
                    price = 42.5m,
                    billingCycle = "Monthly",
                    discountPercentage = 10m,
                    isActive = true,
                    isPublished = true,
                    summary = "Catálogo",
                    rulesNotes = "Regra teste",
                    benefits = new[]
                    {
                        new { sortOrder = 0, title = "Desconto loja", description = "10%" },
                    },
                });
            var res = await _client.SendAsync(post);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var created = await res.Content.ReadFromJsonAsync<JsonElement>();
            var planId = Guid.Parse(created.GetProperty("planId").GetString()!);

            using var get = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/plans/{planId}");
            get.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRes = await _client.SendAsync(get);
            Assert.Equal(HttpStatusCode.OK, getRes.StatusCode);
            var detail = await getRes.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(name, detail.GetProperty("name").GetString());
            Assert.True(detail.GetProperty("isPublished").GetBoolean());
            Assert.True(detail.TryGetProperty("publishedAt", out var pubAt));
            Assert.NotEqual(JsonValueKind.Null, pubAt.ValueKind);
            Assert.True(detail.TryGetProperty("benefits", out var ben));
            Assert.Equal(JsonValueKind.Array, ben.ValueKind);
            Assert.NotEmpty(ben.EnumerateArray());

            using var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/plans/{planId}");
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            put.Content = JsonContent.Create(
                new
                {
                    name = name + " v2",
                    price = 50m,
                    billingCycle = "Yearly",
                    discountPercentage = 0m,
                    isActive = true,
                    isPublished = false,
                    summary = (string?)null,
                    rulesNotes = (string?)null,
                    benefits = Array.Empty<object>(),
                });
            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);

            using var get2 = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/plans/{planId}");
            get2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRes2 = await _client.SendAsync(get2);
            var detail2 = await getRes2.Content.ReadFromJsonAsync<JsonElement>();
            Assert.False(detail2.GetProperty("isPublished").GetBoolean());
            Assert.Equal(JsonValueKind.Null, detail2.GetProperty("publishedAt").ValueKind);
        }
    }

    [Fact]
    public async Task Create_plan_rejects_publish_when_inactive()
    {
        var token = await LoginAdminAsync();
        using var post = new HttpRequestMessage(HttpMethod.Post, "/api/admin/plans");
        post.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        post.Content = JsonContent.Create(
            new
            {
                name = "Invalid",
                price = 1m,
                billingCycle = "Quarterly",
                discountPercentage = 0m,
                isActive = false,
                isPublished = true,
                benefits = Array.Empty<object>(),
            });
        var res = await _client.SendAsync(post);
        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Update_plan_not_found_returns_404()
    {
        var token = await LoginAdminAsync();
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        using var put = new HttpRequestMessage(HttpMethod.Put, $"/api/admin/plans/{id}");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        put.Content = JsonContent.Create(MinimalPlanBody());
        var res = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    private static object MinimalPlanBody() =>
        new
        {
            name = "Minimal",
            price = 1m,
            billingCycle = "Monthly",
            discountPercentage = 0m,
            isActive = true,
            isPublished = false,
            benefits = Array.Empty<object>(),
        };

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
