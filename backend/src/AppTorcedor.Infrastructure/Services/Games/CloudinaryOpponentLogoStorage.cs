using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Games;

public sealed class CloudinaryOpponentLogoStorage(
    ICloudinaryAssetGateway cloudinary,
    IOptions<OpponentLogoStorageOptions> options) : IOpponentLogoStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<string?> SaveOpponentLogoAsync(
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
        var publicId = Guid.NewGuid().ToString("N");
        var upload = await cloudinary
            .UploadAsync(
                new CloudinaryUploadRequest(
                    buffered,
                    string.IsNullOrWhiteSpace(fileName) ? "opponent-logo" : fileName,
                    folder,
                    publicId,
                    CloudinaryAssetResourceType.Image,
                    Overwrite: false),
                cancellationToken)
            .ConfigureAwait(false);

        return upload?.SecureUrl;
    }
}
