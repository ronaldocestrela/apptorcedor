using AppTorcedor.Infrastructure.Services.Cloudinary;

namespace AppTorcedor.Api.Tests;

/// <summary>Fake Cloudinary gateway: upload returns v2 URL with same folder/public_id; records deletes.</summary>
public sealed class RecordingCloudinaryProfilePhotoGateway : ICloudinaryAssetGateway
{
    public List<CloudinaryDeleteRequest> DeleteCalls { get; } = [];

    public Task<CloudinaryUploadResult?> UploadAsync(CloudinaryUploadRequest request, CancellationToken cancellationToken = default)
    {
        var folder = request.Folder.Trim('/');
        var url = $"https://res.cloudinary.com/test/image/upload/v2/{folder}/{request.PublicId}.jpg";
        return Task.FromResult<CloudinaryUploadResult?>(new CloudinaryUploadResult(url));
    }

    public Task<bool> DeleteAsync(CloudinaryDeleteRequest request, CancellationToken cancellationToken = default)
    {
        DeleteCalls.Add(request);
        return Task.FromResult(true);
    }

    public Task<Stream?> OpenReadAsync(string url, CancellationToken cancellationToken = default)
        => Task.FromResult<Stream?>(null);
}
