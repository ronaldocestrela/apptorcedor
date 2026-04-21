using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Account;

public sealed class CloudinaryProfilePhotoStorage(
    ICloudinaryAssetGateway cloudinary,
    IOptions<ProfilePhotoStorageOptions> options) : IProfilePhotoStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<string?> SaveProfilePhotoAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType.Trim()))
            return null;

        var max = Math.Max(1, options.Value.MaxBytes);
        if (content.CanSeek && content.Length > max)
            return null;

        await using var buffered = new MemoryStream();
        await content.CopyToAsync(buffered, cancellationToken).ConfigureAwait(false);
        if (buffered.Length > max)
            return null;
        buffered.Position = 0;

        var folder = options.Value.Cloudinary.Folder.Trim('/');
        var upload = await cloudinary
            .UploadAsync(
                new CloudinaryUploadRequest(
                    buffered,
                    string.IsNullOrWhiteSpace(fileName) ? "profile-photo" : fileName,
                    folder,
                    userId.ToString("N"),
                    CloudinaryAssetResourceType.Image,
                    Overwrite: true),
                cancellationToken)
            .ConfigureAwait(false);

        return upload?.SecureUrl;
    }

    public async Task<bool> DeleteProfilePhotoAsync(string photoUrl, CancellationToken cancellationToken = default)
    {
        var publicId = ExtractPublicId(photoUrl);
        if (publicId is null)
            return false;

        return await cloudinary
            .DeleteAsync(new CloudinaryDeleteRequest(publicId, CloudinaryAssetResourceType.Image), cancellationToken)
            .ConfigureAwait(false);
    }

    public bool ShouldDeletePreviousAfterReplace(string? previousUrl, string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            return false;
        if (string.IsNullOrWhiteSpace(previousUrl))
            return false;
        if (string.Equals(previousUrl, newUrl, StringComparison.OrdinalIgnoreCase))
            return false;

        var prevId = ExtractPublicId(previousUrl);
        var newId = ExtractPublicId(newUrl);
        if (prevId is not null && newId is not null)
            return !string.Equals(prevId, newId, StringComparison.Ordinal);

        // Mixed or unparseable URLs: fall back to string inequality (e.g. local path → cloud URL).
        return !string.Equals(previousUrl, newUrl, StringComparison.OrdinalIgnoreCase);
    }

    private string? ExtractPublicId(string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return null;

        if (!Uri.TryCreate(photoUrl, UriKind.Absolute, out var uri))
            return null;

        var folder = options.Value.Cloudinary.Folder.Trim('/');
        var marker = $"/{folder}/";
        var idx = uri.AbsolutePath.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
            return null;

        var afterFolder = uri.AbsolutePath[(idx + marker.Length)..];
        if (string.IsNullOrWhiteSpace(afterFolder))
            return null;

        var slash = afterFolder.LastIndexOf('/');
        var file = slash >= 0 ? afterFolder[(slash + 1)..] : afterFolder;
        var dot = file.LastIndexOf('.');
        var fileWithoutExt = dot > 0 ? file[..dot] : file;
        var subpath = slash >= 0 ? afterFolder[..slash] + "/" + fileWithoutExt : fileWithoutExt;

        return $"{folder}/{subpath}";
    }
}
