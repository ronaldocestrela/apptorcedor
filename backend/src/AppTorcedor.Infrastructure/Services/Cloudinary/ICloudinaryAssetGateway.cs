namespace AppTorcedor.Infrastructure.Services.Cloudinary;

public interface ICloudinaryAssetGateway
{
    Task<CloudinaryUploadResult?> UploadAsync(CloudinaryUploadRequest request, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(CloudinaryDeleteRequest request, CancellationToken cancellationToken = default);

    Task<Stream?> OpenReadAsync(string url, CancellationToken cancellationToken = default);
}

public sealed record CloudinaryUploadRequest(
    Stream Content,
    string FileName,
    string Folder,
    string PublicId,
    CloudinaryAssetResourceType ResourceType,
    bool Overwrite);

public sealed record CloudinaryUploadResult(string SecureUrl);

public sealed record CloudinaryDeleteRequest(string PublicId, CloudinaryAssetResourceType ResourceType);

public enum CloudinaryAssetResourceType
{
    Image,
    Raw,
}
