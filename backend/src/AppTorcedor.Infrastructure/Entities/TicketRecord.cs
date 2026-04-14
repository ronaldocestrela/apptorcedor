using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class TicketRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid GameId { get; set; }
    public string? ExternalTicketId { get; set; }
    public string? QrCode { get; set; }
    public TicketStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? RedeemedAt { get; set; }
}
