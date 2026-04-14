namespace AppTorcedor.Infrastructure.Options;

public sealed class ProfilePhotoStorageOptions
{
    public const string SectionName = "ProfilePhotos";

    /// <summary>Absolute path root for stored files. Empty = {ContentRoot}/wwwroot/uploads/profile-photos.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Max upload size in bytes (default 5 MB).</summary>
    public int MaxBytes { get; set; } = 5 * 1024 * 1024;
}
