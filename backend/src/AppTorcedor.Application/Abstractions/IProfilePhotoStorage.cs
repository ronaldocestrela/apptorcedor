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
}
