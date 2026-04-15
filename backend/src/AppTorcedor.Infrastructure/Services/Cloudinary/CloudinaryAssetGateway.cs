using AppTorcedor.Infrastructure.Options;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Cloudinary;

public sealed class CloudinaryAssetGateway : ICloudinaryAssetGateway
{
    private readonly CloudinaryDotNet.Cloudinary _cloudinary;
    private readonly HttpClient _httpClient;

    public CloudinaryAssetGateway(IOptions<CloudinaryOptions> options, HttpClient httpClient)
    {
        var cfg = options.Value;
        if (string.IsNullOrWhiteSpace(cfg.CloudName) || string.IsNullOrWhiteSpace(cfg.ApiKey) || string.IsNullOrWhiteSpace(cfg.ApiSecret))
            throw new InvalidOperationException("Cloudinary options are not configured.");

        _cloudinary = new CloudinaryDotNet.Cloudinary(new CloudinaryDotNet.Account(cfg.CloudName, cfg.ApiKey, cfg.ApiSecret));
        _cloudinary.Api.Secure = true;
        _httpClient = httpClient;
    }

    public async Task<CloudinaryUploadResult?> UploadAsync(CloudinaryUploadRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Content is null)
            return null;

        var file = new FileDescription(request.FileName, request.Content);
        if (request.ResourceType == CloudinaryAssetResourceType.Raw)
        {
            var raw = await _cloudinary.UploadAsync(
                new RawUploadParams
                {
                    File = file,
                    Folder = request.Folder,
                    PublicId = request.PublicId,
                    Overwrite = request.Overwrite,
                    UniqueFilename = false,
                    UseFilename = false,
                }).ConfigureAwait(false);
            if (raw.StatusCode is not System.Net.HttpStatusCode.OK || string.IsNullOrWhiteSpace(raw.SecureUrl?.ToString()))
                return null;
            return new CloudinaryUploadResult(raw.SecureUrl.ToString());
        }

        var image = await _cloudinary.UploadAsync(
            new ImageUploadParams
            {
                File = file,
                Folder = request.Folder,
                PublicId = request.PublicId,
                Overwrite = request.Overwrite,
                UniqueFilename = false,
                UseFilename = false,
            },
            cancellationToken).ConfigureAwait(false);
        if (image.StatusCode is not System.Net.HttpStatusCode.OK || string.IsNullOrWhiteSpace(image.SecureUrl?.ToString()))
            return null;
        return new CloudinaryUploadResult(image.SecureUrl.ToString());
    }

    public async Task<bool> DeleteAsync(CloudinaryDeleteRequest request, CancellationToken cancellationToken = default)
    {
        var result = await _cloudinary.DestroyAsync(
            new DeletionParams(request.PublicId)
            {
                ResourceType = request.ResourceType == CloudinaryAssetResourceType.Raw ? ResourceType.Raw : ResourceType.Image,
                Invalidate = true,
            }).ConfigureAwait(false);

        return result.Result is "ok" or "not found";
    }

    public async Task<Stream?> OpenReadAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out _))
            return null;

        var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            response.Dispose();
            return null;
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
    }
}
