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
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IMemberPaymentGatewayService memberPaymentGateway,
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

        if (paymentsGatewayMetadata.IsStripeEnabled)
        {
            var gate = await memberPaymentGateway.EnsureMemberGatewayReadyForChargeAsync(tenantContext.TenantId, cancellationToken);
            if (!gate.IsSuccess)
                return Result<Guid>.Fail(gate.Error!);
        }

        var existing = await paymentsRepository.GetActiveSubscriptionByMemberAsync(profile.Id, cancellationToken);
        if (existing is not null)
        {
            if (!string.IsNullOrEmpty(existing.ExternalSubscriptionId))
            {
                await paymentProvider.CancelAsync(
                    PaymentProviderContext.Member,
                    existing.ExternalSubscriptionId,
                    connectedAccountId: null,
                    idempotencyKey: $"cancel:{existing.ExternalSubscriptionId}",
                    cancellationToken);
            }

            existing.MarkStatus(BillingSubscriptionStatus.Canceled);
        }

        var idempotencyKey = $"member-sub:{profile.Id:N}:{plan.Id:N}";
        var meta = new Dictionary<string, string>
        {
            ["tenant_id"] = tenantContext.TenantId.ToString("D"),
            ["member_profile_id"] = profile.Id.ToString("D"),
            ["member_plan_id"] = plan.Id.ToString("D")
        };
        var providerResult = await paymentProvider.CreateSubscriptionAsync(
            new CreateSubscriptionRequest(
                PaymentProviderContext.Member,
                $"{profile.Id:N}:{plan.Id:N}",
                plan.Preco,
                "BRL",
                "month",
                IdempotencyKey: idempotencyKey,
                StripePriceId: null,
                ConnectedAccountId: null,
                CustomerEmail: null,
                ProductName: plan.Nome,
                AdditionalMetadata: meta),
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
