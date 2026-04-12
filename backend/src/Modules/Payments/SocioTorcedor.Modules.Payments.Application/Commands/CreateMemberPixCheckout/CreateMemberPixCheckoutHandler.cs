using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Entities;
using SocioTorcedor.Modules.Payments.Domain.Enums;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberPixCheckout;

public sealed class CreateMemberPixCheckoutHandler(
    ICurrentUserAccessor currentUserAccessor,
    ICurrentTenantContext tenantContext,
    IMemberProfileRepository memberProfileRepository,
    IMemberPlanRepository memberPlanRepository,
    IMemberTenantPaymentsRepository paymentsRepository,
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IMemberPaymentGatewayService memberPaymentGateway,
    IPaymentProvider paymentProvider)
    : ICommandHandler<CreateMemberPixCheckoutCommand, MemberPixCheckoutDto>
{
    public async Task<Result<MemberPixCheckoutDto>> Handle(
        CreateMemberPixCheckoutCommand command,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Result<MemberPixCheckoutDto>.Fail(Error.Failure("Auth.Required", "User is not authenticated."));

        var profile = await memberProfileRepository.GetTrackedByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<MemberPixCheckoutDto>.Fail(Error.NotFound("Member.NotFound", "Member profile not found."));

        var plan = await memberPlanRepository.GetByIdAsync(command.MemberPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
            return Result<MemberPixCheckoutDto>.Fail(Error.NotFound("Plan.NotFound", "Plan not found or inactive."));

        if (plan.Preco <= 0)
            return Result<MemberPixCheckoutDto>.Fail(Error.Failure("Payments.InvalidAmount", "Plan price must be greater than zero."));

        var subscription = await paymentsRepository.GetActiveSubscriptionByMemberAsync(profile.Id, cancellationToken);
        if (subscription is null || subscription.MemberPlanId != plan.Id)
        {
            return Result<MemberPixCheckoutDto>.Fail(
                Error.Failure(
                    "Payments.Subscription.Required",
                    "Subscribe to this plan first (POST /api/payments/member/subscribe)."));
        }

        if (paymentsGatewayMetadata.IsStripeEnabled)
        {
            var gate = await memberPaymentGateway.EnsureMemberGatewayReadyForChargeAsync(tenantContext.TenantId, cancellationToken);
            if (!gate.IsSuccess)
                return Result<MemberPixCheckoutDto>.Fail(gate.Error!);
        }

        var pix = await paymentProvider.CreatePixAsync(
            new CreatePixChargeRequest(
                PaymentProviderContext.Member,
                $"{profile.Id:N}:{plan.Id:N}:{Guid.NewGuid():N}",
                plan.Preco,
                "BRL",
                $"Plano {plan.Nome}",
                ConnectedAccountId: null),
            cancellationToken);

        var invoice = MemberBillingInvoice.Create(
            subscription.Id,
            plan.Preco,
            "BRL",
            PaymentMethodKind.Pix,
            DateTime.UtcNow.AddDays(1),
            BillingInvoiceStatus.Open,
            pix.ExternalChargeId,
            pix.PixCopyPaste);

        await paymentsRepository.AddInvoiceAsync(invoice, cancellationToken);
        await paymentsRepository.SaveChangesAsync(cancellationToken);

        return Result<MemberPixCheckoutDto>.Ok(
            new MemberPixCheckoutDto(
                invoice.Id,
                pix.ExternalChargeId,
                pix.PixCopyPaste,
                pix.ExpiresAt?.UtcDateTime));
    }
}
