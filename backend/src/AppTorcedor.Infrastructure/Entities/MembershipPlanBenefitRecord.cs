namespace AppTorcedor.Infrastructure.Entities;

public sealed class MembershipPlanBenefitRecord
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
}
