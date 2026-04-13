using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Support;

public sealed class LocalSupportTicketAttachmentStorage(
    IHostEnvironment env,
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

        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "Data", "support-attachments")
            : options.Value.RootPath;
        var dir = Path.Combine(root, ticketId.ToString("N"), messageId.ToString("N"));
        Directory.CreateDirectory(dir);

        var ext = ExtensionForContentType(contentType);
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(dir, storedName);
        await File.WriteAllBytesAsync(physicalPath, content, cancellationToken).ConfigureAwait(false);

        return $"{ticketId:N}/{messageId:N}/{storedName}";
    }

    public Stream? OpenRead(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(storageKey))
            return null;

        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "Data", "support-attachments")
            : options.Value.RootPath;
        var full = Path.GetFullPath(Path.Combine(root, storageKey));
        var rootFull = Path.GetFullPath(root);
        if (!full.StartsWith(rootFull, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            return null;

        return File.OpenRead(full);
    }

    private static string ExtensionForContentType(string contentType) =>
        contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "application/pdf" => ".pdf",
            _ => ".bin",
        };
}
