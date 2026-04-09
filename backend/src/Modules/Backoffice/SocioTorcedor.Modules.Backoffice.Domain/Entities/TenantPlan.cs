using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Domain.Entities;

public sealed class TenantPlan : AggregateRoot
{
    private TenantPlan()
    {
    }

    public Guid TenantId { get; private set; }

    public Guid SaaSPlanId { get; private set; }

    public DateTime StartDate { get; private set; }

    public DateTime? EndDate { get; private set; }

    public TenantPlanStatus Status { get; private set; }

    public BillingCycle BillingCycle { get; private set; }

    public static TenantPlan Assign(
        Guid tenantId,
        Guid saasPlanId,
        DateTime startDate,
        DateTime? endDate,
        BillingCycle billingCycle)
    {
        return new TenantPlan
        {
            TenantId = tenantId,
            SaaSPlanId = saasPlanId,
            StartDate = startDate,
            EndDate = endDate,
            Status = TenantPlanStatus.Active,
            BillingCycle = billingCycle
        };
    }

    public void Revoke()
    {
        Status = TenantPlanStatus.Revoked;
    }

    public void Renew(DateTime newEndDate)
    {
        EndDate = newEndDate;
    }
}
