namespace AppTorcedor.Application.Abstractions;

public interface IAuditLogReadPort
{
    Task<IReadOnlyList<AuditLogRowDto>> ListRecentAsync(string? entityType, int take, CancellationToken cancellationToken = default);
}

public sealed record AuditLogRowDto(
    Guid Id,
    Guid? ActorUserId,
    string Action,
    string EntityType,
    string EntityId,
    string? OldValues,
    string? NewValues,
    string? CorrelationId,
    DateTimeOffset CreatedAt);
