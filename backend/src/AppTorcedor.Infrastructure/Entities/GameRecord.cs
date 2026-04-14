namespace AppTorcedor.Infrastructure.Entities;

public sealed class GameRecord
{
    public Guid Id { get; set; }
    public string Opponent { get; set; } = string.Empty;
    public string Competition { get; set; } = string.Empty;
    public DateTimeOffset GameDate { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
}
