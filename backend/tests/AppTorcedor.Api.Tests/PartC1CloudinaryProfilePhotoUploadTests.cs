using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace AppTorcedor.Api.Tests;

public sealed class PartC1CloudinaryProfilePhotoUploadTests : IClassFixture<AppWebApplicationFactoryWithCloudinaryProfilePhoto>
{
    private readonly HttpClient _client;
    private readonly RecordingCloudinaryProfilePhotoGateway _gateway;

    public PartC1CloudinaryProfilePhotoUploadTests(AppWebApplicationFactoryWithCloudinaryProfilePhoto factory)
    {
        _client = factory.CreateClient();
        _gateway = factory.CloudinaryGateway;
    }

    [Fact]
    public async Task Upload_photo_when_previous_url_is_same_cloudinary_public_id_does_not_call_delete()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"photo-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Foto Torcedor",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11999999999",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        var userIdN = Guid.Parse(
            new JwtSecurityTokenHandler().ReadJwtToken(auth!.AccessToken).Claims
                .First(c => c.Type == JwtRegisteredClaimNames.Sub).Value)
            .ToString("N");
        var previousUrl = $"https://res.cloudinary.com/x/image/upload/v1/profile-photos/{userIdN}.jpg";

        _gateway.DeleteCalls.Clear();

        using (var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            put.Content = JsonContent.Create(
                new
                {
                    document = (string?)"00000000000",
                    birthDate = (DateOnly?)new DateOnly(1990, 1, 15),
                    photoUrl = previousUrl,
                    address = (string?)"Rua A",
                });
            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);
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
        }

        Assert.Empty(_gateway.DeleteCalls);
    }

    [Fact]
    public async Task Upload_photo_when_previous_path_differs_in_cloudinary_still_cleans_up_old()
    {
        var req = await _client.GetAsync("/api/account/register/requirements");
        var legal = await req.Content.ReadFromJsonAsync<RequirementsDto>();
        Assert.NotNull(legal);

        var email = $"photo2-{Guid.NewGuid():N}@test.local";
        var register = await _client.PostAsJsonAsync(
            "/api/account/register",
            new
            {
                name = "Foto2 Torcedor",
                email,
                password = "RegisterPass123!",
                phoneNumber = "11999999999",
                acceptedLegalDocumentVersionIds = new[] { legal!.TermsOfUseVersionId, legal.PrivacyPolicyVersionId },
            });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        var auth = await register.Content.ReadFromJsonAsync<AuthResponseDto>();
        Assert.NotNull(auth);

        // Different from this user's public id: delete should be attempted
        const string otherUserN = "bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb";
        var previousUrl = $"https://res.cloudinary.com/x/image/upload/v1/profile-photos/{otherUserN}.jpg";
        _gateway.DeleteCalls.Clear();

        using (var put = new HttpRequestMessage(HttpMethod.Put, "/api/account/profile"))
        {
            put.Headers.Authorization = new AuthenticationHeaderValue("Bearer", auth.AccessToken);
            put.Content = JsonContent.Create(
                new
                {
                    document = (string?)"00000000000",
                    birthDate = (DateOnly?)new DateOnly(1990, 1, 15),
                    photoUrl = previousUrl,
                    address = (string?)"Rua A",
                });
            var putRes = await _client.SendAsync(put);
            Assert.Equal(HttpStatusCode.NoContent, putRes.StatusCode);
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
        }

        Assert.Single(_gateway.DeleteCalls);
        Assert.Equal("profile-photos/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb", _gateway.DeleteCalls[0].PublicId);
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
}
