using AppTorcedor.Application.Abstractions;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class SupportTicketRecord
{
    public Guid Id { get; set; }
    public Guid RequesterUserId { get; set; }
    public Guid? AssignedAgentUserId { get; set; }
    public string Queue { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public SupportTicketPriority Priority { get; set; }
    public SupportTicketStatus Status { get; set; }
    public DateTimeOffset SlaDeadlineUtc { get; set; }
    public DateTimeOffset? FirstResponseAtUtc { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
