namespace AppTorcedor.Infrastructure.Options;

public sealed class TeamShieldStorageOptions
{
    public const string SectionName = "TeamShield";

    /// <summary>Storage provider: Local or Cloudinary.</summary>
    public string Provider { get; set; } = "Local";

    /// <summary>Absolute path root for stored files. Empty = {ContentRoot}/wwwroot/uploads/team-shield.</summary>
    public string RootPath { get; set; } = string.Empty;

    /// <summary>Max upload size in bytes (default 5 MB).</summary>
    public int MaxBytes { get; set; } = 5 * 1024 * 1024;

    public TeamShieldCloudinaryOptions Cloudinary { get; set; } = new();
}

public sealed class TeamShieldCloudinaryOptions
{
    public string Folder { get; set; } = "team-shield";
}
