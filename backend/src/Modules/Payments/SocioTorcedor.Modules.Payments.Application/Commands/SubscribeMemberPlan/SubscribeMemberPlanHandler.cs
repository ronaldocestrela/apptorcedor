using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.SubscribeMemberPlan;

public sealed class SubscribeMemberPlanHandler(
    ICurrentUserAccessor currentUserAccessor,
    ICurrentTenantContext tenantContext,
    IMemberProfileRepository memberProfileRepository,
    IMemberPlanRepository memberPlanRepository,
    IMemberTenantPaymentsRepository paymentsRepository,
    ITenantMasterPaymentsRepository masterPaymentsRepository,
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IPaymentProvider paymentProvider)
    : ICommandHandler<SubscribeMemberPlanCommand, Guid>
{
    public async Task<Result<Guid>> Handle(SubscribeMemberPlanCommand command, CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Result<Guid>.Fail(Error.Failure("Auth.Required", "User is not authenticated."));

        var profile = await memberProfileRepository.GetTrackedByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<Guid>.Fail(Error.NotFound("Member.NotFound", "Member profile not found."));

        var plan = await memberPlanRepository.GetByIdAsync(command.MemberPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
            return Result<Guid>.Fail(Error.NotFound("Plan.NotFound", "Plan not found or inactive."));

        if (plan.Preco <= 0)
            return Result<Guid>.Fail(Error.Failure("Payments.InvalidAmount", "Plan price must be greater than zero."));

        string? connectAccountId = null;
        if (paymentsGatewayMetadata.IsStripeEnabled)
        {
            var connect = await masterPaymentsRepository.GetStripeConnectByTenantIdAsync(tenantContext.TenantId, cancellationToken);
            if (connect is null || !connect.ChargesEnabled)
                return Result<Guid>.Fail(Error.Failure("Payments.Connect.NotReady", "Stripe Connect is not ready for this club."));

            connectAccountId = connect.StripeAccountId;
        }

        var existing = await paymentsRepository.GetActiveSubscriptionByMemberAsync(profile.Id, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrEmpty(existing.ExternalSubscriptionId))
            {
                await paymentProvider.CancelAsync(
                    PaymentProviderContext.Member,
                    existing.ExternalSubscriptionId,
                    connectedAccountId: connectAccountId,
                    idempotencyKey: $"cancel:{existing.ExternalSubscriptionId}",
                    cancellationToken);
            }

            existing.MarkStatus(BillingSubscriptionStatus.Canceled);
        }

        var idempotencyKey = $"member-sub:{profile.Id:N}:{plan.Id:N}";
        var providerResult = await paymentProvider.CreateSubscriptionAsync(
            new CreateSubscriptionRequest(
                PaymentProviderContext.Member,
                $"{profile.Id:N}:{plan.Id:N}",
                plan.Preco,
                "BRL",
                "month",
                IdempotencyKey: idempotencyKey,
                StripePriceId: null,
                ConnectedAccountId: connectAccountId,
                CustomerEmail: null,
                ProductName: plan.Nome),
            cancellationToken);

        var subscription = MemberBillingSubscription.Start(
            profile.Id,
            plan.Id,
            plan.Preco,
            "BRL",
            command.PaymentMethod,
            providerResult.ExternalCustomerId,
            providerResult.ExternalSubscriptionId,
            BillingSubscriptionStatus.Active,
            DateTime.UtcNow.AddMonths(1));

        await paymentsRepository.AddSubscriptionAsync(subscription, cancellationToken);

        var invoice = MemberBillingInvoice.Create(
            subscription.Id,
            plan.Preco,
            "BRL",
            command.PaymentMethod,
            DateTime.UtcNow.AddDays(7),
            BillingInvoiceStatus.Open,
            externalInvoiceId: null,
            pixCopyPaste: null);

        await paymentsRepository.AddInvoiceAsync(invoice, cancellationToken);
        await paymentsRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(subscription.Id);
    }
}
