using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Domain.Enums;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.StartTenantSaasBilling;

public sealed class StartTenantSaasBillingHandler(
    ITenantPlanRepository tenantPlanRepository,
    ISaaSPlanRepository saaSPlanRepository,
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentProvider paymentProvider)
    : ICommandHandler<StartTenantSaasBillingCommand, Guid>
{
    public async Task<Result<Guid>> Handle(StartTenantSaasBillingCommand command, CancellationToken cancellationToken)
    {
        var activePlan = await tenantPlanRepository.GetActiveByTenantIdAsync(command.TenantId, cancellationToken);
        if (activePlan is null)
            return Result<Guid>.Fail(Error.NotFound("TenantPlan.NotFound", "No active tenant plan for this tenant."));

        var saasPlan = await saaSPlanRepository.GetTrackedByIdAsync(activePlan.SaaSPlanId, cancellationToken);
        if (saasPlan is null)
            return Result<Guid>.Fail(Error.NotFound("SaaSPlan.NotFound", "SaaS plan not found."));

        var existing = await paymentsRepository.GetActiveSubscriptionByTenantAsync(command.TenantId, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Fail(Error.Conflict("Payments.Subscription.Exists", "Tenant already has an active billing subscription."));

        var amount = activePlan.BillingCycle == BillingCycle.Yearly
            ? saasPlan.YearlyPrice ?? saasPlan.MonthlyPrice * 12
            : saasPlan.MonthlyPrice;

        if (amount <= 0)
            return Result<Guid>.Fail(Error.Failure("Payments.InvalidAmount", "Plan amount must be greater than zero."));

        var interval = activePlan.BillingCycle == BillingCycle.Yearly ? "year" : "month";
        var providerResult = await paymentProvider.CreateSubscriptionAsync(
            new CreateSubscriptionRequest(
                PaymentProviderContext.SaaS,
                command.TenantId.ToString("N"),
                amount,
                "BRL",
                interval),
            cancellationToken);

        var subscription = TenantBillingSubscription.Start(
            command.TenantId,
            activePlan.Id,
            activePlan.SaaSPlanId,
            activePlan.BillingCycle,
            amount,
            "BRL",
            providerResult.ExternalCustomerId,
            providerResult.ExternalSubscriptionId,
            BillingSubscriptionStatus.Active,
            NextBillingUtc(activePlan.BillingCycle));

        await paymentsRepository.AddSubscriptionAsync(subscription, cancellationToken);

        var invoice = TenantBillingInvoice.Create(
            subscription.Id,
            amount,
            "BRL",
            DateTime.UtcNow.AddDays(7),
            BillingInvoiceStatus.Open,
            externalInvoiceId: null);

        await paymentsRepository.AddInvoiceAsync(invoice, cancellationToken);
        await paymentsRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(subscription.Id);
    }

    private static DateTime? NextBillingUtc(BillingCycle cycle) =>
        cycle == BillingCycle.Yearly
            ? DateTime.UtcNow.AddYears(1)
            : DateTime.UtcNow.AddMonths(1);
}
