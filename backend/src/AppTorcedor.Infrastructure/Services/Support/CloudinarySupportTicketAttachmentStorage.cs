using System.Text;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using AppTorcedor.Infrastructure.Services.Cloudinary;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Support;

public sealed class CloudinarySupportTicketAttachmentStorage(
    ICloudinaryAssetGateway cloudinary,
    IOptions<SupportTicketAttachmentStorageOptions> options) : ISupportTicketAttachmentStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
        "application/pdf",
    };

    public async Task<string?> SaveAsync(
        Guid ticketId,
        Guid messageId,
        byte[] content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        if (content is null || content.Length == 0)
            return null;
        if (string.IsNullOrWhiteSpace(contentType) || !AllowedContentTypes.Contains(contentType.Trim()))
            return null;

        var max = Math.Max(1, options.Value.MaxBytesPerFile);
        if (content.Length > max)
            return null;

        var type = ResolveResourceType(contentType);
        var folder = options.Value.Cloudinary.Folder.Trim('/');
        var publicId = $"{ticketId:N}/{messageId:N}/{Guid.NewGuid():N}";

        await using var stream = new MemoryStream(content, writable: false);
        var uploaded = await cloudinary
            .UploadAsync(
                new CloudinaryUploadRequest(
                    stream,
                    string.IsNullOrWhiteSpace(fileName) ? "attachment" : fileName,
                    folder,
                    publicId,
                    type,
                    Overwrite: false),
                cancellationToken)
            .ConfigureAwait(false);

        if (uploaded is null)
            return null;

        return BuildStorageKey(type, $"{folder}/{publicId}", uploaded.SecureUrl);
    }

    public async Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var parsed = ParseStorageKey(storageKey);
        if (parsed is null)
            return null;

        return await cloudinary.OpenReadAsync(parsed.Value.Url, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var parsed = ParseStorageKey(storageKey);
        if (parsed is null)
            return;

        await cloudinary
            .DeleteAsync(new CloudinaryDeleteRequest(parsed.Value.PublicId, parsed.Value.ResourceType), cancellationToken)
            .ConfigureAwait(false);
    }

    private static CloudinaryAssetResourceType ResolveResourceType(string contentType)
        => contentType.Trim().Equals("application/pdf", StringComparison.OrdinalIgnoreCase)
            ? CloudinaryAssetResourceType.Raw
            : CloudinaryAssetResourceType.Image;

    private static string BuildStorageKey(CloudinaryAssetResourceType resourceType, string publicId, string url)
        => $"cloudinary|{ToToken(resourceType)}|{publicId}|{Uri.EscapeDataString(url)}";

    private static (CloudinaryAssetResourceType ResourceType, string PublicId, string Url)? ParseStorageKey(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
            return null;

        var parts = storageKey.Split('|', 4, StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || !parts[0].Equals("cloudinary", StringComparison.OrdinalIgnoreCase))
            return null;

        var type = parts[1].Equals("raw", StringComparison.OrdinalIgnoreCase)
            ? CloudinaryAssetResourceType.Raw
            : parts[1].Equals("image", StringComparison.OrdinalIgnoreCase)
                ? CloudinaryAssetResourceType.Image
                : (CloudinaryAssetResourceType?)null;
        if (type is null)
            return null;

        var publicId = parts[2];
        if (string.IsNullOrWhiteSpace(publicId))
            return null;

        string url;
        try
        {
            url = Uri.UnescapeDataString(parts[3]);
        }
        catch (Exception)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(url))
            return null;

        return (type.Value, publicId, url);
    }

    private static string ToToken(CloudinaryAssetResourceType type)
        => type == CloudinaryAssetResourceType.Raw ? "raw" : "image";
}
