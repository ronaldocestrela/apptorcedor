namespace AppTorcedor.Application.Abstractions;

/// <summary>Stores profile photos and returns a public URL path (e.g. under /uploads/...).</summary>
public interface IProfilePhotoStorage
{
    /// <returns>Public relative URL, or null if validation/storage failed.</returns>
    Task<string?> SaveProfilePhotoAsync(
        Guid userId,
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a previously stored profile photo URL when supported by the provider.</summary>
    Task<bool> DeleteProfilePhotoAsync(string photoUrl, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if <paramref name="previousUrl"/> should be removed after a successful replace with <paramref name="newUrl"/>.
    /// For providers that reuse a stable id with overwrite (e.g. Cloudinary), the URLs can differ (version segment) while still referring to the same asset.
    /// </summary>
    bool ShouldDeletePreviousAfterReplace(string? previousUrl, string newUrl);
}
