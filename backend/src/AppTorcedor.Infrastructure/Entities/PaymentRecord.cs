namespace AppTorcedor.Infrastructure.Entities;

public sealed class PaymentRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid MembershipId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTimeOffset DueDate { get; set; }
    public DateTimeOffset? PaidAt { get; set; }
    public string? PaymentMethod { get; set; }
    public string? ExternalReference { get; set; }
    public string? ProviderName { get; set; }
    public DateTimeOffset? CancelledAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? LastProviderSyncAt { get; set; }
    public string? StatusReason { get; set; }
}
