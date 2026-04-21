using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Account;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Tests;

public sealed class CloudinaryProfilePhotoStorageTests
{
    [Fact]
    public async Task SaveProfilePhotoAsync_uploads_and_returns_secure_url()
    {
        var gateway = new FakeCloudinaryGateway
        {
            NextUploadResult = new CloudinaryUploadResult("https://cdn.example/profile.jpg"),
        };
        var sut = new CloudinaryProfilePhotoStorage(
            gateway,
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions
                {
                    MaxBytes = 1024,
                    Cloudinary = new ProfilePhotoCloudinaryOptions { Folder = "profile-photos" },
                }));

        await using var stream = new MemoryStream([1, 2, 3]);
        var url = await sut.SaveProfilePhotoAsync(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            stream,
            "photo.jpg",
            "image/jpeg");

        Assert.Equal("https://cdn.example/profile.jpg", url);
        Assert.Single(gateway.UploadCalls);
        var call = gateway.UploadCalls[0];
        Assert.Equal(CloudinaryAssetResourceType.Image, call.ResourceType);
        Assert.True(call.Overwrite);
        Assert.Equal("profile-photos", call.Folder);
        Assert.Equal("11111111111111111111111111111111", call.PublicId);
    }

    [Fact]
    public async Task SaveProfilePhotoAsync_rejects_invalid_content_type()
    {
        var gateway = new FakeCloudinaryGateway();
        var sut = new CloudinaryProfilePhotoStorage(
            gateway,
            Microsoft.Extensions.Options.Options.Create(new ProfilePhotoStorageOptions { MaxBytes = 1024 }));

        await using var stream = new MemoryStream([1, 2, 3]);
        var url = await sut.SaveProfilePhotoAsync(Guid.NewGuid(), stream, "photo.txt", "text/plain");

        Assert.Null(url);
        Assert.Empty(gateway.UploadCalls);
    }

    [Fact]
    public async Task DeleteProfilePhotoAsync_deletes_when_url_matches_configured_folder()
    {
        var gateway = new FakeCloudinaryGateway();
        var sut = new CloudinaryProfilePhotoStorage(
            gateway,
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions
                {
                    Cloudinary = new ProfilePhotoCloudinaryOptions { Folder = "profile-photos" },
                }));

        var ok = await sut.DeleteProfilePhotoAsync(
            "https://res.cloudinary.com/demo/image/upload/v1/profile-photos/11111111111111111111111111111111.jpg");

        Assert.True(ok);
        Assert.Single(gateway.DeleteCalls);
        var call = gateway.DeleteCalls[0];
        Assert.Equal("profile-photos/11111111111111111111111111111111", call.PublicId);
        Assert.Equal(CloudinaryAssetResourceType.Image, call.ResourceType);
    }

    [Fact]
    public void ShouldDeletePreviousAfterReplace_same_public_id_different_version_returns_false()
    {
        var sut = new CloudinaryProfilePhotoStorage(
            new FakeCloudinaryGateway(),
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions
                {
                    Cloudinary = new ProfilePhotoCloudinaryOptions { Folder = "profile-photos" },
                }));

        const string prev =
            "https://res.cloudinary.com/demo/image/upload/v1/profile-photos/11111111111111111111111111111111.jpg";
        const string next =
            "https://res.cloudinary.com/demo/image/upload/v99/profile-photos/11111111111111111111111111111111.jpg";

        Assert.False(sut.ShouldDeletePreviousAfterReplace(prev, next));
    }

    [Fact]
    public void ShouldDeletePreviousAfterReplace_different_public_id_returns_true()
    {
        var sut = new CloudinaryProfilePhotoStorage(
            new FakeCloudinaryGateway(),
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions
                {
                    Cloudinary = new ProfilePhotoCloudinaryOptions { Folder = "profile-photos" },
                }));

        var prev =
            "https://res.cloudinary.com/demo/image/upload/v1/profile-photos/aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa.jpg";
        var next =
            "https://res.cloudinary.com/demo/image/upload/v1/profile-photos/bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb.jpg";

        Assert.True(sut.ShouldDeletePreviousAfterReplace(prev, next));
    }

    [Fact]
    public void ShouldDeletePreviousAfterReplace_equal_urls_returns_false()
    {
        var sut = new CloudinaryProfilePhotoStorage(
            new FakeCloudinaryGateway(),
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions
                {
                    Cloudinary = new ProfilePhotoCloudinaryOptions { Folder = "profile-photos" },
                }));

        const string url = "https://res.cloudinary.com/x/image/upload/v1/profile-photos/aaa.jpg";
        Assert.False(sut.ShouldDeletePreviousAfterReplace(url, url));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void ShouldDeletePreviousAfterReplace_with_empty_previous_returns_false(string? previous)
    {
        var sut = new CloudinaryProfilePhotoStorage(
            new FakeCloudinaryGateway(),
            Microsoft.Extensions.Options.Options.Create(
                new ProfilePhotoStorageOptions
                {
                    Cloudinary = new ProfilePhotoCloudinaryOptions { Folder = "profile-photos" },
                }));

        Assert.False(sut.ShouldDeletePreviousAfterReplace(previous, "https://res.cloudinary.com/x/y.jpg"));
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
