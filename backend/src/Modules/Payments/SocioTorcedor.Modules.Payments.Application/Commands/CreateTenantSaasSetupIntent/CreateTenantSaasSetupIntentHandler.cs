using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasSetupIntent;

public sealed class CreateTenantSaasSetupIntentHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentProvider paymentProvider)
    : ICommandHandler<CreateTenantSaasSetupIntentCommand, TenantSaasSetupIntentDto>
{
    public async Task<Result<TenantSaasSetupIntentDto>> Handle(
        CreateTenantSaasSetupIntentCommand command,
        CancellationToken cancellationToken)
    {
        var sub = await paymentsRepository.GetActiveSubscriptionByTenantAsync(command.TenantId, cancellationToken);
        if (sub is null || string.IsNullOrWhiteSpace(sub.ExternalCustomerId))
        {
            return Result<TenantSaasSetupIntentDto>.Fail(
                Error.NotFound(
                    "Payments.Subscription.NotFound",
                    "No active SaaS subscription with a payment customer."));
        }

        try
        {
            var si = await paymentProvider.CreateSaasSetupIntentAsync(
                new CreateSaasSetupIntentRequest(
                    sub.ExternalCustomerId!,
                    $"saas-si:{command.TenantId:N}:{Guid.NewGuid():N}"),
                cancellationToken);

            return Result<TenantSaasSetupIntentDto>.Ok(
                new TenantSaasSetupIntentDto(si.ClientSecret, si.SetupIntentId));
        }
        catch (Exception ex)
        {
            return Result<TenantSaasSetupIntentDto>.Fail(Error.Failure("Payments.Provider.Error", ex.Message));
        }
    }
}
