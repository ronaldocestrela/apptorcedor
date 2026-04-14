namespace AppTorcedor.Infrastructure.Auditing;

public sealed class CurrentAuditContext : ICurrentAuditContext
{
    public Guid? UserId { get; set; }
    public string? CorrelationId { get; set; }
}
