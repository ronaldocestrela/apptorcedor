using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AppTorcedor.Infrastructure.Testing;
using Xunit;

namespace AppTorcedor.Api.Tests;

public sealed class BrandingApiTests(AppWebApplicationFactory factory) : IClassFixture<AppWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_branding_anonymous_returns_ok_with_null_when_not_configured()
    {
        var res = await _client.GetAsync("/api/branding");
        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        var body = await res.Content.ReadFromJsonAsync<PublicBrandingDto>();
        Assert.NotNull(body);
        Assert.Null(body!.TeamShieldUrl);
    }

    [Fact]
    public async Task Admin_upload_team_shield_then_public_branding_returns_url()
    {
        var token = await LoginAdminAsync();
        using var uploadContent = new MultipartFormDataContent();
        var jpeg = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x00]);
        jpeg.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(jpeg, "file", "shield.jpg");
        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/admin/config/team-shield");
        uploadReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        uploadReq.Content = uploadContent;
        var uploadRes = await _client.SendAsync(uploadReq);
        Assert.Equal(HttpStatusCode.OK, uploadRes.StatusCode);
        var uploaded = await uploadRes.Content.ReadFromJsonAsync<TeamShieldUploadDto>();
        Assert.False(string.IsNullOrEmpty(uploaded?.TeamShieldUrl));

        var brandingRes = await _client.GetAsync("/api/branding");
        Assert.Equal(HttpStatusCode.OK, brandingRes.StatusCode);
        var branding = await brandingRes.Content.ReadFromJsonAsync<PublicBrandingDto>();
        Assert.Equal(uploaded!.TeamShieldUrl, branding?.TeamShieldUrl);
    }

    [Fact]
    public async Task Upload_team_shield_forbidden_for_torcedor()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = TestingSeedConstants.TorcedorEmail, password = "TestPassword123!" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.NotNull(tokens);

        using var uploadContent = new MultipartFormDataContent();
        var jpeg = new ByteArrayContent([0xFF, 0xD8, 0xFF, 0xDB, 0x00, 0x00]);
        jpeg.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
        uploadContent.Add(jpeg, "file", "shield.jpg");
        using var uploadReq = new HttpRequestMessage(HttpMethod.Post, "/api/admin/config/team-shield");
        uploadReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokens!.AccessToken);
        uploadReq.Content = uploadContent;
        var uploadRes = await _client.SendAsync(uploadReq);
        Assert.Equal(HttpStatusCode.Forbidden, uploadRes.StatusCode);
    }

    private async Task<string> LoginAdminAsync()
    {
        var login = await _client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = "admin@test.local", password = "TestPassword123!" });
        login.EnsureSuccessStatusCode();
        var tokens = await login.Content.ReadFromJsonAsync<AuthTokensDto>();
        Assert.False(string.IsNullOrEmpty(tokens?.AccessToken));
        return tokens!.AccessToken;
    }

    private sealed record PublicBrandingDto(string? TeamShieldUrl);

    private sealed record TeamShieldUploadDto(string TeamShieldUrl);

    private sealed record AuthTokensDto(string AccessToken, string RefreshToken, int ExpiresIn, IReadOnlyList<string> Roles);
}
