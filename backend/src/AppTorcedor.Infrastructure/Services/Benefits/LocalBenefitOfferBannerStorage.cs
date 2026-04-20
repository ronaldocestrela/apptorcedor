using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Benefits;

public sealed class LocalBenefitOfferBannerStorage(
    IHostEnvironment env,
    IOptions<BenefitOfferBannerStorageOptions> options) : IBenefitOfferBannerStorage
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg",
        "image/png",
        "image/webp",
    };

    public const string UrlPrefix = "/uploads/benefit-offer-banners/";

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

        var ext = ExtensionForContentType(contentType);
        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "benefit-offer-banners")
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

        return $"{UrlPrefix}{storedName}";
    }

    public Task TryDeleteBannerAsync(string? bannerUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(bannerUrl))
            return Task.CompletedTask;

        if (!bannerUrl.StartsWith(UrlPrefix, StringComparison.OrdinalIgnoreCase))
            return Task.CompletedTask;

        var relative = bannerUrl[UrlPrefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "benefit-offer-banners")
            : options.Value.RootPath;
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var allowedRoot = Path.GetFullPath(root);
        if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            return Task.CompletedTask;

        try
        {
            File.Delete(full);
        }
        catch
        {
            /* ignore */
        }

        return Task.CompletedTask;
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
