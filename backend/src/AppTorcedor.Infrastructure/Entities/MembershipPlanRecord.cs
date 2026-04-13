namespace AppTorcedor.Infrastructure.Entities;

public sealed class MembershipPlanRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = "Monthly";
    public decimal DiscountPercentage { get; set; }
    public bool IsActive { get; set; } = true;
}
