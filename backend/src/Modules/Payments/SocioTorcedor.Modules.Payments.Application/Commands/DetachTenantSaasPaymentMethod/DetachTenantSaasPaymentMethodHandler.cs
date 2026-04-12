using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;

namespace SocioTorcedor.Modules.Payments.Application.Commands.DetachTenantSaasPaymentMethod;

public sealed class DetachTenantSaasPaymentMethodHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentProvider paymentProvider)
    : ICommandHandler<DetachTenantSaasPaymentMethodCommand>
{
    public async Task<Result> Handle(
        DetachTenantSaasPaymentMethodCommand command,
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

        var customerId = sub.ExternalCustomerId!;
        var pmId = command.PaymentMethodId.Trim();

        try
        {
            var existing = await paymentProvider.ListSaasCustomerPaymentMethodsAsync(
                new ListSaasCustomerPaymentMethodsRequest(customerId),
                cancellationToken);

            if (existing.Items.All(x => x.Id != pmId))
            {
                return Result.Fail(
                    Error.NotFound("Payments.PaymentMethod.NotFound", "Payment method not found for this account."));
            }

            if (existing.Items.Count == 1)
            {
                return Result.Fail(
                    Error.Conflict(
                        "Payments.PaymentMethod.LastCard",
                        "Cannot remove the only saved card. Add another card first."));
            }

            await paymentProvider.DetachSaasPaymentMethodAsync(
                new DetachSaasPaymentMethodRequest(customerId, pmId),
                cancellationToken);

            return Result.Ok();
        }
        catch (Exception ex)
        {
            return Result.Fail(Error.Failure("Payments.Provider.Error", ex.Message));
        }
    }
}
