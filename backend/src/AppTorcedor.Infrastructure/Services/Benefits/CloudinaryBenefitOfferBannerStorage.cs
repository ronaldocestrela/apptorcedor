using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Benefits;

public sealed class CloudinaryBenefitOfferBannerStorage(
    ICloudinaryAssetGateway cloudinary,
    IOptions<BenefitOfferBannerStorageOptions> options) : IBenefitOfferBannerStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public async Task<string?> SaveBannerAsync(
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
                    string.IsNullOrWhiteSpace(fileName) ? "benefit-banner" : fileName,
                    folder,
                    publicId,
                    CloudinaryAssetResourceType.Image,
                    Overwrite: false),
                cancellationToken)
            .ConfigureAwait(false);

        return upload?.SecureUrl;
    }

    public async Task TryDeleteBannerAsync(string? bannerUrl, CancellationToken cancellationToken = default)
    {
        var publicId = ExtractPublicId(bannerUrl);
        if (publicId is null)
            return;

        await cloudinary
            .DeleteAsync(new CloudinaryDeleteRequest(publicId, CloudinaryAssetResourceType.Image), cancellationToken)
            .ConfigureAwait(false);
    }

    private string? ExtractPublicId(string? photoUrl)
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
