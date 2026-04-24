using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AppTorcedor.Identity;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class PartC1TorcedorAccountTests
    : IClassFixture<AppWebApplicationFactoryWithLegalSeed>, IClassFixture<AppWebApplicationFactoryWithFakeGoogle>
{
    private readonly HttpClient _client;
    private readonly HttpClient _clientFakeGoogle;

    public PartC1TorcedorAccountTests(
        AppWebApplicationFactoryWithLegalSeed factory,
        AppWebApplicationFactoryWithFakeGoogle factoryGoogle)
    {
        _client = factory.CreateClient();
        _clientFakeGoogle = factoryGoogle.CreateClient();
    }

    [Fact]
    public async Task Register_requirements_returns_published_version_ids()
    {
        var res = await _client.GetAsync("/api/account/register/requirements");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.TermsOfUseVersionId);
        Assert.NotEqual(Guid.Empty, body.PrivacyPolicyVersionId);
    }

    [Fact]
    public async Task Register_rejects_weak_password()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"new-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Novo Torcedor",
                email,
                password = "short",
                phoneNumber = "11999999999",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);
    }

    [Fact]
    public async Task Register_rejects_empty_phone()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"no-phone-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Novo Torcedor",
                email,
                password = "RegisterPass123!",
                phoneNumber = (string?)"   ",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        Assert.Equal(HttpStatusCode.BadRequest, register.StatusCode);
        var err = await register.Content.ReadFromJsonAsync<JsonElement>();
        var errors = err.GetProperty("errors");
        Assert.Equal(JsonValueKind.Array, errors.ValueKind);
        var first = errors[0].GetString() ?? string.Empty;
        Assert.Contains("Celular", first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_returns_tokens_and_profile_flow_works()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"new-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Novo Torcedor",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11999999999",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.Contains(SystemRoles.Torcedor, auth!.Roles);

        using (var profileGet = new HttpRequestMessage(HttpMethod.Get, "/api/account/profile"))
        {
            profileGet.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var pr = await _client.SendAsync(profileGet);
            Assert.Equal(HttpStatusCode.OK, pr.StatusCode);
            var prof = await pr.Content.ReadFromJsonAsync<ProfileDto>();
            Assert.Null(prof?.Document);
        }

        using (var meReq = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me"))
        {
            meReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var meRes = await _client.SendAsync(meReq);
            Assert.Equal(HttpStatusCode.OK, meRes.StatusCode);
            var me = await meRes.Content.ReadFromJsonAsync<MeDto>();
            Assert.True(me?.RequiresProfileCompletion);
        }

        using (var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            put.Content = JsonContent.Create(
                new { document = "390.533.447-05", birthDate = (DateOnly?)new DateOnly(1990, 1, 15), photoUrl = (string?)null, address = "Rua A" });
            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);
        }

        using (var profileGet2 = new HttpRequestMessage(HttpMethod.Get, "/api/account/profile"))
        {
            profileGet2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var pr2 = await _client.SendAsync(profileGet2);
            var prof2 = await pr2.Content.ReadFromJsonAsync<ProfileDto>();
            Assert.Equal("39053344705", prof2?.Document);
        }

        using (var me2 = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me"))
        {
            me2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            var meRes = await _client.SendAsync(me2);
            var me = await meRes.Content.ReadFromJsonAsync<MeDto>();
            Assert.False(me?.RequiresProfileCompletion ?? true);
        }

        using var photoContent = new MultipartFormDataContent();
        var jpeg = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x00]);
        jpeg.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        photoContent.Add(jpeg, "file", "tiny.jpg");
        using (var photoReq = new HttpRequestMessage(HttpMethod.Post, "/api/account/profile/photo"))
        {
            photoReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            photoReq.Content = photoContent;
            var photoRes = await _client.SendAsync(photoReq);
            Assert.Equal(HttpStatusCode.OK, photoRes.StatusCode);
            var photoBody = await photoRes.Content.ReadFromJsonAsync<PhotoUploadDto>();
            Assert.False(string.IsNullOrEmpty(photoBody?.PhotoUrl));
        }
    }

    [Fact]
    public async Task Profile_rejects_invalid_cpf()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"bad-cpf-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Cpf",
                email,
                password = "RegisterPass123!",
                phoneNumber = (string?)"11",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        put.Content = JsonContent.Create(
            new
            {
                document = (string?)"12345678901",
                birthDate = (DateOnly?)new DateOnly(1990, 1, 15),
                photoUrl = (string?)null,
                address = (string?)"Rua A",
            });
        var putRes = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.BadRequest, putRes.StatusCode);
        var err = await putRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("cpf_invalid", err.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Profile_rejects_cpf_when_already_held_by_another_account()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"dup-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Duplicata",
                email,
                password = "RegisterPass123!",
                phoneNumber = (string?)"11",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        register.EnsureSuccessStatusCode();
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        using var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile");
        put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth!.AccessToken);
        // Mesmo CPF do usuário de seed (member) — deve conflitar.
        put.Content = JsonContent.Create(
            new
            {
                document = (string?)"111.444.777-35",
                birthDate = (DateOnly?)new DateOnly(1992, 3, 3),
                photoUrl = (string?)null,
                address = (string?)"B",
            });
        var putRes = await _client.SendAsync(put);
        Assert.Equal(HttpStatusCode.Conflict, putRes.StatusCode);
        var err = await putRes.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("cpf_already_in_use", err.GetProperty("error").GetString());
    }

    [Fact]
    public async Task Google_sign_in_creates_user_with_consents()
    {
        var req = await _clientFakeGoogle.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var google = await _clientFakeGoogle.PostAsJsonAsync(
            "/api/auth/google",
            new
            {
                idToken = FakeGoogleIdTokenValidator.ValidTestToken,
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        Assert.Equal(HttpStatusCode.OK, google.StatusCode);
        var auth = await google.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);
        Assert.Contains(SystemRoles.Torcedor, auth!.Roles);

        var second = await _clientFakeGoogle.PostAsJsonAsync(
            "/api/auth/google",
            new { idToken = FakeGoogleIdTokenValidator.ValidTestToken, acceptedLegalDocumentVersionIds = Array.Empty<Guid>() });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    private sealed record RequirementsDto(
        Guid TermsOfUseVersionId,
        Guid PrivacyPolicyVersionId,
        string TermsTitle,
        string PrivacyTitle);

    private sealed record AuthResponseDto(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        IReadOnlyList<string> Roles);

    private sealed record ProfileDto(string? Document, DateOnly? BirthDate, string? PhotoUrl, string? Address);

    private sealed record MeDto(
        Guid Id,
        string Email,
        string Name,
        IReadOnlyList<string> Roles,
        IReadOnlyList<string> Permissions,
        bool RequiresProfileCompletion);

    private sealed record PhotoUploadDto(string PhotoUrl);
}
