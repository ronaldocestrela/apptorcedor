namespace AppTorcedor.Infrastructure.Entities;

public sealed class AppConfigurationEntry
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = "{}";
    public int Version { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public Guid? UpdatedByUserId { get; set; }
}
