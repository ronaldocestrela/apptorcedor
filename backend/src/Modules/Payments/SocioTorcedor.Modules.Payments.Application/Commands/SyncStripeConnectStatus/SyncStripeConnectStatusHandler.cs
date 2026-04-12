using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.SyncStripeConnectStatus;

public sealed class SyncStripeConnectStatusHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IPaymentProvider paymentProvider)
    : ICommandHandler<SyncStripeConnectStatusCommand, StripeConnectStatusDto>
{
    public async Task<Result<StripeConnectStatusDto>> Handle(
        SyncStripeConnectStatusCommand command,
        CancellationToken cancellationToken)
    {
        if (!paymentsGatewayMetadata.IsStripeEnabled)
            return Result<StripeConnectStatusDto>.Fail(Error.Failure("Payments.Stripe.Disabled", "Stripe is not configured."));

        var row = await paymentsRepository.GetStripeConnectByTenantIdAsync(command.TenantId, cancellationToken);
        if (row is null)
            return Result<StripeConnectStatusDto>.Fail(Error.NotFound("Payments.Connect.NotFound", "Stripe Connect account is not set up for this tenant."));

        var live = await paymentProvider.GetConnectAccountStatusAsync(row.StripeAccountId, cancellationToken);
        row.SyncFromStripe(live.ChargesEnabled, live.PayoutsEnabled, live.DetailsSubmitted);
        await paymentsRepository.SaveChangesAsync(cancellationToken);

        return Result<StripeConnectStatusDto>.Ok(
            new StripeConnectStatusDto(
                true,
                row.StripeAccountId,
                (int)row.OnboardingStatus,
                row.ChargesEnabled,
                row.PayoutsEnabled,
                row.DetailsSubmitted));
    }
}
