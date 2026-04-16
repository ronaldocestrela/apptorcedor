using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Games;

public sealed class LocalOpponentLogoStorage(
    IHostEnvironment env,
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

        var ext = ExtensionForContentType(contentType);
        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "opponent-logos")
            : options.Value.RootPath;
        Directory.CreateDirectory(root);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(root, storedName);

        long written = 0;
        var oversize = false;
        var buffer = new byte[81920];
        await using (var fs = new FileStream(physicalPath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
        {
            int read;
            while ((read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false)) > 0)
            {
                written += read;
                if (written > max)
                {
                    oversize = true;
                    break;
                }

                await fs.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
            }
        }

        if (oversize)
        {
            try
            {
                File.Delete(physicalPath);
            }
            catch
            {
                /* ignore */
            }

            return null;
        }

        return $"/uploads/opponent-logos/{storedName}";
    }

    private static string ExtensionForContentType(string contentType) =>
        contentType.Trim().ToLowerInvariant() switch
        {
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".bin",
        };
}
