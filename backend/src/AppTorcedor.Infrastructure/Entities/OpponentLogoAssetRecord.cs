namespace AppTorcedor.Infrastructure.Entities;

public sealed class OpponentLogoAssetRecord
{
    public Guid Id { get; set; }
    public string PublicUrl { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}
