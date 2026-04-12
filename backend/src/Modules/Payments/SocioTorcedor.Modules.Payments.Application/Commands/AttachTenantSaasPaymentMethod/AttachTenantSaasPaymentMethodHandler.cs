using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;

namespace SocioTorcedor.Modules.Payments.Application.Commands.AttachTenantSaasPaymentMethod;

public sealed class AttachTenantSaasPaymentMethodHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentProvider paymentProvider)
    : ICommandHandler<AttachTenantSaasPaymentMethodCommand>
{
    public async Task<Result> Handle(
        AttachTenantSaasPaymentMethodCommand command,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.PaymentMethodId))
        {
            return Result.Fail(Error.Validation("Payments.PaymentMethod.Required", "paymentMethodId is required."));
        }

        var sub = await paymentsRepository.GetActiveSubscriptionByTenantAsync(command.TenantId, cancellationToken);
        if (sub is null || string.IsNullOrWhiteSpace(sub.ExternalCustomerId))
        {
            return Result.Fail(
                Error.NotFound(
                    "Payments.Subscription.NotFound",
                    "No active SaaS subscription with a payment customer."));
        }

        try
        {
            await paymentProvider.AttachSaasPaymentMethodAsync(
                new AttachSaasPaymentMethodRequest(
                    sub.ExternalCustomerId!,
                    command.PaymentMethodId.Trim(),
                    command.SetAsDefault,
                    sub.ExternalSubscriptionId,
                    $"saas-pm:{command.TenantId:N}:{Guid.NewGuid():N}"),
                cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(Error.Failure("Payments.Provider.Error", ex.Message));
        }
    }
}
