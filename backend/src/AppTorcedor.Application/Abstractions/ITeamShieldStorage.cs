namespace AppTorcedor.Application.Abstractions;

/// <summary>Stores the club team shield image and returns a public URL (relative under /uploads/... or absolute Cloudinary URL).</summary>
public interface ITeamShieldStorage
{
    /// <returns>Public URL path or absolute URL, or null if validation/storage failed.</returns>
    Task<string?> SaveTeamShieldAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteTeamShieldAsync(string shieldUrl, CancellationToken cancellationToken = default);
}
