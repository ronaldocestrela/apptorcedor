using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasBillingPortalSession;

public sealed class CreateTenantSaasBillingPortalSessionHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IPaymentProvider paymentProvider)
    : ICommandHandler<CreateTenantSaasBillingPortalSessionCommand, TenantSaasPortalSessionDto>
{
    public async Task<Result<TenantSaasPortalSessionDto>> Handle(
        CreateTenantSaasBillingPortalSessionCommand command,
        CancellationToken cancellationToken)
    {
        if (!paymentsGatewayMetadata.IsStripeEnabled)
            return Result<TenantSaasPortalSessionDto>.Fail(Error.Failure("Payments.Stripe.Disabled", "Stripe is not configured."));

        var sub = await paymentsRepository.GetActiveSubscriptionByTenantAsync(command.TenantId, cancellationToken);
        if (sub is null || string.IsNullOrWhiteSpace(sub.ExternalCustomerId))
            return Result<TenantSaasPortalSessionDto>.Fail(Error.NotFound("Payments.Subscription.NotFound", "No active SaaS subscription with Stripe customer."));

        var portal = await paymentProvider.CreateBillingPortalSessionAsync(
            new CreateBillingPortalSessionRequest(
                sub.ExternalCustomerId,
                command.ReturnUrl,
                IdempotencyKey: $"saas-portal:{command.TenantId:N}:{Guid.NewGuid():N}"),
            cancellationToken);

        return Result<TenantSaasPortalSessionDto>.Ok(new TenantSaasPortalSessionDto(portal.Url));
    }
}
