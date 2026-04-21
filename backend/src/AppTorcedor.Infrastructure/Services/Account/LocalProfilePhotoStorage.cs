using AppTorcedor.Application.Abstractions;
using AppTorcedor.Infrastructure.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace AppTorcedor.Infrastructure.Services.Account;

public sealed class LocalProfilePhotoStorage(
    IHostEnvironment env,
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

        var ext = ExtensionForContentType(contentType);
        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "profile-photos")
            : options.Value.RootPath;
        var userDir = Path.Combine(root, userId.ToString("N"));
        Directory.CreateDirectory(userDir);

        var storedName = $"{Guid.NewGuid():N}{ext}";
        var physicalPath = Path.Combine(userDir, storedName);

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

        return $"/uploads/profile-photos/{userId:N}/{storedName}";
    }

    public Task<bool> DeleteProfilePhotoAsync(string photoUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            return Task.FromResult(false);

        const string prefix = "/uploads/profile-photos/";
        if (!photoUrl.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(false);

        var relative = photoUrl[prefix.Length..].Replace('/', Path.DirectorySeparatorChar);
        var root = string.IsNullOrWhiteSpace(options.Value.RootPath)
            ? Path.Combine(env.ContentRootPath, "wwwroot", "uploads", "profile-photos")
            : options.Value.RootPath;
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var allowedRoot = Path.GetFullPath(root);
        if (!full.StartsWith(allowedRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(full))
            return Task.FromResult(false);

        try
        {
            File.Delete(full);
            return Task.FromResult(true);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    public bool ShouldDeletePreviousAfterReplace(string? previousUrl, string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            return false;
        if (string.IsNullOrWhiteSpace(previousUrl))
            return false;
        if (string.Equals(previousUrl, newUrl, StringComparison.OrdinalIgnoreCase))
            return false;
        // Each local upload is a new file name; a replace always targets a different path.
        return true;
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
