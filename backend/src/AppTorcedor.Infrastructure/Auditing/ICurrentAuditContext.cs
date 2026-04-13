namespace AppTorcedor.Infrastructure.Auditing;

public interface ICurrentAuditContext
{
    Guid? UserId { get; set; }
    string? CorrelationId { get; set; }
}
