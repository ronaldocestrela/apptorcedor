using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetTenantSaasBilling;

public sealed class GetTenantSaasBillingHandler(ITenantMasterPaymentsRepository repository)
    : IQueryHandler<GetTenantSaasBillingQuery, TenantSaasBillingSubscriptionDto?>
{
    public async Task<Result<TenantSaasBillingSubscriptionDto?>> Handle(
        GetTenantSaasBillingQuery query,
        CancellationToken cancellationToken)
    {
        var sub = await repository.GetActiveSubscriptionByTenantAsync(query.TenantId, cancellationToken);
        if (sub is null)
            return Result<TenantSaasBillingSubscriptionDto?>.Ok(null);

        var dto = new TenantSaasBillingSubscriptionDto(
            sub.Id,
            sub.TenantId,
            sub.TenantPlanId,
            sub.SaaSPlanId,
            sub.BillingCycle,
            sub.RecurringAmount,
            sub.Currency,
            sub.Status,
            sub.ExternalCustomerId,
            sub.ExternalSubscriptionId,
            sub.NextBillingAtUtc,
            sub.CreatedAtUtc);

        return Result<TenantSaasBillingSubscriptionDto?>.Ok(dto);
    }
}
