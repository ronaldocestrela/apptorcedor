using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.BuildingBlocks.Shared.Tenancy;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateMemberStripeCheckoutSession;

public sealed class CreateMemberStripeCheckoutSessionHandler(
    ICurrentUserAccessor currentUserAccessor,
    ICurrentTenantContext tenantContext,
    IMemberProfileRepository memberProfileRepository,
    IMemberPlanRepository memberPlanRepository,
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IMemberPaymentGatewayService memberPaymentGateway,
    IPaymentProvider paymentProvider)
    : ICommandHandler<CreateMemberStripeCheckoutSessionCommand, MemberStripeCheckoutSessionDto>
{
    public async Task<Result<MemberStripeCheckoutSessionDto>> Handle(
        CreateMemberStripeCheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        if (!paymentsGatewayMetadata.IsStripeEnabled)
            return Result<MemberStripeCheckoutSessionDto>.Fail(Error.Failure("Payments.Stripe.Disabled", "Stripe is not configured."));

        var userId = currentUserAccessor.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Result<MemberStripeCheckoutSessionDto>.Fail(Error.Failure("Auth.Required", "User is not authenticated."));

        var profile = await memberProfileRepository.GetTrackedByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<MemberStripeCheckoutSessionDto>.Fail(Error.NotFound("Member.NotFound", "Member profile not found."));

        var plan = await memberPlanRepository.GetByIdAsync(command.MemberPlanId, cancellationToken);
        if (plan is null || !plan.IsActive)
            return Result<MemberStripeCheckoutSessionDto>.Fail(Error.NotFound("Plan.NotFound", "Plan not found or inactive."));

        if (plan.Preco <= 0)
            return Result<MemberStripeCheckoutSessionDto>.Fail(Error.Failure("Payments.InvalidAmount", "Plan price must be greater than zero."));

        var gate = await memberPaymentGateway.EnsureMemberGatewayReadyForChargeAsync(tenantContext.TenantId, cancellationToken);
        if (!gate.IsSuccess)
            return Result<MemberStripeCheckoutSessionDto>.Fail(gate.Error!);

        var metadata = new Dictionary<string, string>
        {
            ["tenant_id"] = tenantContext.TenantId.ToString("D"),
            ["member_profile_id"] = profile.Id.ToString("D"),
            ["member_plan_id"] = plan.Id.ToString("D")
        };

        var session = await paymentProvider.CreateCheckoutSessionAsync(
            new CreateCheckoutSessionRequest(
                PaymentProviderContext.Member,
                Mode: "subscription",
                Amount: plan.Preco,
                Currency: "BRL",
                ProductName: plan.Nome,
                BillingInterval: "month",
                SuccessUrl: command.SuccessUrl,
                CancelUrl: command.CancelUrl,
                Metadata: metadata,
                ConnectedAccountId: null,
                CustomerEmail: null,
                IdempotencyKey: $"member-checkout:{profile.Id:N}:{plan.Id:N}:{Guid.NewGuid():N}"),
            cancellationToken);

        return Result<MemberStripeCheckoutSessionDto>.Ok(new MemberStripeCheckoutSessionDto(session.SessionId, session.Url));
    }
}
