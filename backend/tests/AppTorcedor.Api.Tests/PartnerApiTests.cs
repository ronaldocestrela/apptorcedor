using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Controllers;
using AppTorcedor.Infrastructure.Testing;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartnerApiTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    // ─── AdminPartnerKeys ────────────────────────────────────────────────────

    [Fact]
    public async Task List_partner_keys_returns_401_without_jwt()
    {
        var res = await _client.GetAsync("/api/admin/partner-keys");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task List_partner_keys_returns_403_for_torcedor()
    {
        var token = await LoginTorcedorAsync();
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/admin/partner-keys");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_list_and_revoke_partner_key()
    {
        var adminToken = await LoginAdminAsync();

        // Criar
        string plaintextKey;
        Guid keyId;
        using (var create = new HttpRequestMessage(HttpMethod.Post, "/api/admin/partner-keys"))
        {
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            create.Content = JsonContent.Create(new { name = "Parceiro Teste" });
            var res = await _client.SendAsync(create);
            Assert.Equal(HttpStatusCode.Created, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            plaintextKey = body.GetProperty("plaintextKey").GetString()!;
            keyId = Guid.Parse(body.GetProperty("id").GetString()!);
            Assert.StartsWith("sk_partner_", plaintextKey);
            Assert.False(string.IsNullOrEmpty(body.GetProperty("keyPrefix").GetString()));
        }

        // Listar — deve aparecer na lista
        using (var list = new HttpRequestMessage(HttpMethod.Get, "/api/admin/partner-keys"))
        {
            list.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var res = await _client.SendAsync(list);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var items = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(JsonValueKind.Array, items.ValueKind);
            var found = items.EnumerateArray().Any(e => e.GetProperty("id").GetString() == keyId.ToString());
            Assert.True(found, "Created key should appear in the list.");
            // Nenhum item deve expor a chave em texto claro
            Assert.All(items.EnumerateArray(), e => Assert.False(e.TryGetProperty("plaintextKey", out _)));
        }

        // Lookup funciona com a chave criada
        using (var lookup = new HttpRequestMessage(HttpMethod.Get, "/api/partner/v1/lookup?phone=00000000000"))
        {
            lookup.Headers.Add("X-Api-Key", plaintextKey);
            var res = await _client.SendAsync(lookup);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(body.TryGetProperty("exists", out _));
            Assert.True(body.TryGetProperty("isActiveMember", out _));
        }

        // Revogar
        using (var revoke = new HttpRequestMessage(HttpMethod.Delete, $"/api/admin/partner-keys/{keyId}"))
        {
            revoke.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            var res = await _client.SendAsync(revoke);
            Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        }

        // Lookup com chave revogada deve retornar 401
        using (var lookup2 = new HttpRequestMessage(HttpMethod.Get, "/api/partner/v1/lookup?phone=00000000000"))
        {
            lookup2.Headers.Add("X-Api-Key", plaintextKey);
            var res = await _client.SendAsync(lookup2);
            Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
        }
    }

    // ─── PartnerController ───────────────────────────────────────────────────

    [Fact]
    public async Task Lookup_returns_401_without_api_key()
    {
        var res = await _client.GetAsync("/api/partner/v1/lookup?phone=11999999999");
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Lookup_returns_401_with_invalid_api_key()
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/partner/v1/lookup?phone=11999999999");
        req.Headers.Add("X-Api-Key", "sk_partner_invalid_fake_key_xyz");
        var res = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Lookup_returns_400_without_phone_param()
    {
        // Cria key válida primeiro
        var adminToken = await LoginAdminAsync();
        string plaintextKey;
        using (var create = new HttpRequestMessage(HttpMethod.Post, "/api/admin/partner-keys"))
        {
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            create.Content = JsonContent.Create(new { name = "Parceiro 400 Test" });
            var res = await _client.SendAsync(create);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            plaintextKey = body.GetProperty("plaintextKey").GetString()!;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/partner/v1/lookup");
        req.Headers.Add("X-Api-Key", plaintextKey);
        var lookupRes = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.BadRequest, lookupRes.StatusCode);
    }

    [Fact]
    public async Task Lookup_returns_false_for_unknown_phone()
    {
        var adminToken = await LoginAdminAsync();
        string plaintextKey;
        using (var create = new HttpRequestMessage(HttpMethod.Post, "/api/admin/partner-keys"))
        {
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            create.Content = JsonContent.Create(new { name = "Parceiro Lookup Test" });
            var res = await _client.SendAsync(create);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            plaintextKey = body.GetProperty("plaintextKey").GetString()!;
        }

        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/partner/v1/lookup?phone=00099900099");
        req.Headers.Add("X-Api-Key", plaintextKey);
        var lookupRes = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, lookupRes.StatusCode);
        var resultBody = await lookupRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(resultBody.GetProperty("exists").GetBoolean());
        Assert.False(resultBody.GetProperty("isActiveMember").GetBoolean());
    }

    [Fact]
    public async Task Lookup_returns_true_for_known_torcedor_phone()
    {
        var adminToken = await LoginAdminAsync();
        string plaintextKey;
        using (var create = new HttpRequestMessage(HttpMethod.Post, "/api/admin/partner-keys"))
        {
            create.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
            create.Content = JsonContent.Create(new { name = "Parceiro Torcedor Test" });
            var res = await _client.SendAsync(create);
            res.EnsureSuccessStatusCode();
            var body = await res.Content.ReadFromJsonAsync<JsonElement>();
            plaintextKey = body.GetProperty("plaintextKey").GetString()!;
        }

        // O seed cria um torcedor com PhoneNumber via TorcedorAccountService; usamos o torcedor de amostra
        var torcedorPhone = TestingSeedConstants.TorcedorPhone;

        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/partner/v1/lookup?phone={torcedorPhone}");
        req.Headers.Add("X-Api-Key", plaintextKey);
        var lookupRes = await _client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, lookupRes.StatusCode);
        var resultBody = await lookupRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(resultBody.GetProperty("exists").GetBoolean());
    }

    [Fact]
    public void Admin_partner_keys_controller_has_split_read_and_manage_policies()
    {
        var methods = typeof(AdminPartnerKeysController).GetMethods();

        var list = methods.Single(m => m.Name == nameof(AdminPartnerKeysController.List));
        var listAuth = list.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(Policies.WebhooksRead, listAuth.Policy);

        var create = methods.Single(m => m.Name == nameof(AdminPartnerKeysController.Create));
        var createAuth = create.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(Policies.PermissionPrefix + AppTorcedor.Identity.ApplicationPermissions.WebhooksGerenciar, createAuth.Policy);

        var revoke = methods.Single(m => m.Name == nameof(AdminPartnerKeysController.Revoke));
        var revokeAuth = revoke.GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single();
        Assert.Equal(Policies.PermissionPrefix + AppTorcedor.Identity.ApplicationPermissions.WebhooksGerenciar, revokeAuth.Policy);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

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

    private async Task<string> LoginTorcedorAsync()
    {
        using var login = new HttpRequestMessage(HttpMethod.Post, "/api/auth/login")
        {
            Content = JsonContent.Create(new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" }),
        };
        var res = await _client.SendAsync(login);
        res.EnsureSuccessStatusCode();
        var body = await res.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("accessToken").GetString()!;
    }
}
