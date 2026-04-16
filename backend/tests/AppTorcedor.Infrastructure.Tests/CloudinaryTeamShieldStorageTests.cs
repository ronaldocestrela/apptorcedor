using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Branding;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using MEO = Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class CloudinaryTeamShieldStorageTests
{
    [Fact]
    public async Task SaveTeamShieldAsync_uploads_with_fixed_public_id_and_overwrite()
    {
        var gateway = new FakeCloudinaryGateway
        {
            NextUploadResult = new CloudinaryUploadResult("https://cdn.example/shield.jpg"),
        };
        var sut = new CloudinaryTeamShieldStorage(
            gateway,
            MEO.Options.Create(
                new TeamShieldStorageOptions
                {
                    MaxBytes = 1024,
                    Cloudinary = new TeamShieldCloudinaryOptions { Folder = "team-shield" },
                }));

        await using var stream = new MemoryStream([1, 2, 3]);
        var url = await sut.SaveTeamShieldAsync(stream, "shield.png", "image/png");

        Assert.Equal("https://cdn.example/shield.jpg", url);
        Assert.Single(gateway.UploadCalls);
        var call = gateway.UploadCalls[0];
        Assert.Equal(CloudinaryAssetResourceType.Image, call.ResourceType);
        Assert.True(call.Overwrite);
        Assert.Equal("team-shield", call.Folder);
        Assert.Equal("club", call.PublicId);
    }

    [Fact]
    public async Task SaveTeamShieldAsync_rejects_invalid_content_type()
    {
        var gateway = new FakeCloudinaryGateway();
        var sut = new CloudinaryTeamShieldStorage(
            gateway,
            MEO.Options.Create(new TeamShieldStorageOptions { MaxBytes = 1024 }));

        await using var stream = new MemoryStream([1, 2, 3]);
        var url = await sut.SaveTeamShieldAsync(stream, "x.txt", "text/plain");

        Assert.Null(url);
        Assert.Empty(gateway.UploadCalls);
    }

    [Fact]
    public async Task DeleteTeamShieldAsync_deletes_when_url_matches_configured_folder()
    {
        var gateway = new FakeCloudinaryGateway();
        var sut = new CloudinaryTeamShieldStorage(
            gateway,
            MEO.Options.Create(
                new TeamShieldStorageOptions
                {
                    Cloudinary = new TeamShieldCloudinaryOptions { Folder = "team-shield" },
                }));

        var ok = await sut.DeleteTeamShieldAsync(
            "https://res.cloudinary.com/demo/image/upload/v1/team-shield/club.jpg");

        Assert.True(ok);
        Assert.Single(gateway.DeleteCalls);
        var call = gateway.DeleteCalls[0];
        Assert.Equal("team-shield/club", call.PublicId);
        Assert.Equal(CloudinaryAssetResourceType.Image, call.ResourceType);
    }

    private sealed class FakeCloudinaryGateway : ICloudinaryAssetGateway
    {
        public CloudinaryUploadResult? NextUploadResult { get; set; }
        public List<CloudinaryUploadRequest> UploadCalls { get; } = [];
        public List<CloudinaryDeleteRequest> DeleteCalls { get; } = [];

        public Task<CloudinaryUploadResult?> UploadAsync(CloudinaryUploadRequest request, CancellationToken cancellationToken = default)
        {
            UploadCalls.Add(request);
            return Task.FromResult(NextUploadResult);
        }

        public Task<bool> DeleteAsync(CloudinaryDeleteRequest request, CancellationToken cancellationToken = default)
        {
            DeleteCalls.Add(request);
            return Task.FromResult(true);
        }

        public Task<Stream?> OpenReadAsync(string url, CancellationToken cancellationToken = default)
            => Task.FromResult<Stream?>(new MemoryStream([0x42]));
    }
}
