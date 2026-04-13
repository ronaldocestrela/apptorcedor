using AppTorcedor.Identity;

namespace AppTorcedor.Infrastructure.Entities;

public sealed class MembershipRecord
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? PlanId { get; set; }
    public MembershipStatus Status { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset? EndDate { get; set; }
    public DateTimeOffset? NextDueDate { get; set; }
}
