using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Application.Commands.StartStripeConnectOnboarding;

public sealed class StartStripeConnectOnboardingHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentsGatewayMetadata paymentsGatewayMetadata,
    IPaymentProvider paymentProvider)
    : ICommandHandler<StartStripeConnectOnboardingCommand, StripeOnboardingLinkDto>
{
    public async Task<Result<StripeOnboardingLinkDto>> Handle(
        StartStripeConnectOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        if (!paymentsGatewayMetadata.IsStripeEnabled)
            return Result<StripeOnboardingLinkDto>.Fail(Error.Failure("Payments.Stripe.Disabled", "Stripe is not configured."));

        var existing = await paymentsRepository.GetStripeConnectByTenantIdAsync(command.TenantId, cancellationToken);
        string accountId;
        if (existing is null)
        {
            var created = await paymentProvider.CreateConnectExpressAccountAsync(
                new CreateConnectExpressAccountRequest(
                    Country: "BR",
                    Email: null,
                    Metadata: new Dictionary<string, string> { ["tenant_id"] = command.TenantId.ToString("D") },
                    IdempotencyKey: $"connect-acct:{command.TenantId:N}"),
                cancellationToken);

            accountId = created.AccountId;
            var row = TenantStripeConnectAccount.Create(command.TenantId, accountId);
            await paymentsRepository.AddStripeConnectAsync(row, cancellationToken);
            await paymentsRepository.SaveChangesAsync(cancellationToken);
        }
        else
        {
            accountId = existing.StripeAccountId;
        }

        var link = await paymentProvider.CreateConnectAccountLinkAsync(
            new CreateConnectAccountLinkRequest(
                accountId,
                command.RefreshUrl,
                command.ReturnUrl,
                IdempotencyKey: $"connect-link:{command.TenantId:N}:{Guid.NewGuid():N}"),
            cancellationToken);

        return Result<StripeOnboardingLinkDto>.Ok(new StripeOnboardingLinkDto(link.Url));
    }
}
