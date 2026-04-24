using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace AppTorcedor.Api.Tests;

/// <summary>
/// Testes de integração que verificam se os consentimentos LGPD são de fato persistidos no banco
/// após o cadastro público (email/senha e Google).
/// </summary>
public sealed class PartC1LgpdConsentRecordingTests
    : IClassFixture<AppWebApplicationFactoryWithLegalSeed>,
      IClassFixture<AppWebApplicationFactoryWithFakeGoogle>
{
    private readonly HttpClient _client;
    private readonly HttpClient _googleClient;

    public PartC1LgpdConsentRecordingTests(
        AppWebApplicationFactoryWithLegalSeed factory,
        AppWebApplicationFactoryWithFakeGoogle googleFactory)
    {
        _client = factory.CreateClient();
        _googleClient = googleFactory.CreateClient();
    }

    // ─── Cadastro email/senha ────────────────────────────────────────────────────

    [Fact]
    public async Task Register_stores_exactly_two_lgpd_consents_in_database()
    {
        var legal = await GetRequirementsAsync(_client);

        var auth = await RegisterAsync(_client, legal);

        var userId = await GetUserIdAsync(_client, auth.AccessToken);
        var consents = await GetConsentsAsync(_client, userId);

        Assert.Equal(2, consents.GetArrayLength());
    }

    [Fact]
    public async Task Register_stores_consent_for_terms_of_use_version()
    {
        var legal = await GetRequirementsAsync(_client);

        var auth = await RegisterAsync(_client, legal);

        var userId = await GetUserIdAsync(_client, auth.AccessToken);
        var consents = await GetConsentsAsync(_client, userId);

        var versionIds = Enumerable.Range(0, consents.GetArrayLength())
            .Select(i => consents[i].GetProperty("legalDocumentVersionId").GetGuid())
            .ToList();

        Assert.Contains(legal.TermsOfUseVersionId, versionIds);
    }

    [Fact]
    public async Task Register_stores_consent_for_privacy_policy_version()
    {
        var legal = await GetRequirementsAsync(_client);

        var auth = await RegisterAsync(_client, legal);

        var userId = await GetUserIdAsync(_client, auth.AccessToken);
        var consents = await GetConsentsAsync(_client, userId);

        var versionIds = Enumerable.Range(0, consents.GetArrayLength())
            .Select(i => consents[i].GetProperty("legalDocumentVersionId").GetGuid())
            .ToList();

        Assert.Contains(legal.PrivacyPolicyVersionId, versionIds);
    }

    [Fact]
    public async Task Register_returns_400_when_accepted_version_ids_are_empty()
    {
        var res = await _client.PostAsJsonAsync("/api/account/register", new
        {
            name = "Test",
            email = $"no-ids-{Guid.NewGuid():N}@test.local",
            password = "SomePass123!",
            phoneNumber = "11999999999",
            acceptedLegalDocumentVersionIds = Array.Empty<Guid>(),
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_returns_400_when_accepted_version_ids_do_not_match_published_versions()
    {
        var res = await _client.PostAsJsonAsync("/api/account/register", new
        {
            name = "Test",
            email = $"wrong-ids-{Guid.NewGuid():N}@test.local",
            password = "SomePass123!",
            phoneNumber = "11999999999",
            acceptedLegalDocumentVersionIds = new[] { Guid.NewGuid(), Guid.NewGuid() },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_returns_400_when_only_terms_consent_is_provided()
    {
        var legal = await GetRequirementsAsync(_client);

        var res = await _client.PostAsJsonAsync("/api/account/register", new
        {
            name = "Test",
            email = $"only-terms-{Guid.NewGuid():N}@test.local",
            password = "SomePass123!",
            phoneNumber = "11999999999",
            acceptedLegalDocumentVersionIds = new[] { legal.TermsOfUseVersionId },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_returns_400_when_only_privacy_consent_is_provided()
    {
        var legal = await GetRequirementsAsync(_client);

        var res = await _client.PostAsJsonAsync("/api/account/register", new
        {
            name = "Test",
            email = $"only-privacy-{Guid.NewGuid():N}@test.local",
            password = "SomePass123!",
            phoneNumber = "11999999999",
            acceptedLegalDocumentVersionIds = new[] { legal.PrivacyPolicyVersionId },
        });

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    // ─── Cadastro via Google ──────────────────────────────────────────────────────

    [Fact]
    public async Task Google_sign_in_stores_exactly_two_lgpd_consents_in_database()
    {
        var legal = await GetRequirementsAsync(_googleClient);

        var googleRes = await _googleClient.PostAsJsonAsync("/api/auth/google", new
        {
            idToken = FakeGoogleIdTokenValidator.ValidTestToken,
            acceptedLegalDocumentVersionIds = new[] { legal.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
        });
        Assert.Equal(HttpStatusCode.OK, googleRes.StatusCode);
        var auth = await googleRes.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotNull(auth);

        var userId = await GetUserIdAsync(_googleClient, auth!.AccessToken);
        var consents = await GetConsentsAsync(_googleClient, userId);

        Assert.Equal(2, consents.GetArrayLength());
    }

    [Fact]
    public async Task Google_sign_in_returns_401_when_accepted_version_ids_are_empty_and_user_is_new()
    {
        // Sem IDs aceitos, novos usuários não podem ser criados via Google.
        var res = await _googleClient.PostAsJsonAsync("/api/auth/google", new
        {
            idToken = FakeGoogleIdTokenValidator.ValidTestToken,
            acceptedLegalDocumentVersionIds = Array.Empty<Guid>(),
        });

        // Retorna 401 pois não há sessão para emitir (usuário não criado nem autenticado).
        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    // ─── Helpers privados ────────────────────────────────────────────────────────

    private static async Task<RequirementsDto> GetRequirementsAsync(HttpClient client)
    {
        var res = await client.GetAsync("/api/account/register/requirements");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var dto = await res.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(dto);
        return dto!;
    }

    private static async Task<AuthTokensDto> RegisterAsync(HttpClient client, RequirementsDto legal)
    {
        var res = await client.PostAsJsonAsync("/api/account/register", new
        {
            name = "Consent Recorder",
            email = $"cr-{Guid.NewGuid():N}@test.local",
            password = "ConsentPass123!",
            phoneNumber = "11999999999",
            acceptedLegalDocumentVersionIds = new[] { legal.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
        });
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var auth = await res.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotNull(auth);
        return auth!;
    }

    private static async Task<Guid> GetUserIdAsync(HttpClient client, string accessToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var me = await res.Content.ReadFromJsonAsync<MeDto>();
        Assert.NotNull(me);
        return me!.Id;
    }

    private static async Task<JsonElement> GetConsentsAsync(HttpClient client, Guid userId)
    {
        var adminToken = await LoginAdminAsync(client);
        using var req = new HttpRequestMessage(HttpMethod.Get, $"/api/admin/lgpd/users/{userId}/consents");
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var res = await client.SendAsync(req);
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        return await res.Content.ReadFromJsonAsync<JsonElement>();
    }

    private static async Task<string> LoginAdminAsync(HttpClient client)
    {
        var res = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        res.EnsureSuccessStatusCode();
        var tokens = await res.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotNull(tokens);
        return tokens!.AccessToken;
    }

    // ─── DTOs locais ─────────────────────────────────────────────────────────────

    private sealed record RequirementsDto(
        Guid TermsOfUseVersionId,
        Guid PrivacyPolicyVersionId,
        string TermsTitle,
        string PrivacyTitle);

    private sealed record AuthTokensDto(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        IReadOnlyList<string> Roles);

    private sealed record MeDto(
        Guid Id,
        string Email,
        string Name,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        bool RequiresProfileCompletion);
}
