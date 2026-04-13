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
}
