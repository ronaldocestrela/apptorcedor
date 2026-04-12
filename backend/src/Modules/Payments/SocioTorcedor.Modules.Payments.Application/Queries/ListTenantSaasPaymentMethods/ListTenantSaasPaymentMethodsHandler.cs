using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Application.Payments;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasPaymentMethods;

public sealed class ListTenantSaasPaymentMethodsHandler(
    ITenantMasterPaymentsRepository paymentsRepository,
    IPaymentProvider paymentProvider)
    : IQueryHandler<ListTenantSaasPaymentMethodsQuery, IReadOnlyList<TenantSaasPaymentMethodDto>>
{
    public async Task<Result<IReadOnlyList<TenantSaasPaymentMethodDto>>> Handle(
        ListTenantSaasPaymentMethodsQuery query,
        CancellationToken cancellationToken)
    {
        var sub = await paymentsRepository.GetActiveSubscriptionByTenantAsync(query.TenantId, cancellationToken);
        if (sub is null || string.IsNullOrWhiteSpace(sub.ExternalCustomerId))
        {
            return Result<IReadOnlyList<TenantSaasPaymentMethodDto>>.Fail(
                Error.NotFound(
                    "Payments.Subscription.NotFound",
                    "No active SaaS subscription with a payment customer."));
        }

        try
        {
            var list = await paymentProvider.ListSaasCustomerPaymentMethodsAsync(
                new ListSaasCustomerPaymentMethodsRequest(sub.ExternalCustomerId!),
                cancellationToken);

            var dto = list.Items
                .Select(pm => new TenantSaasPaymentMethodDto(
                    pm.Id,
                    pm.Brand,
                    pm.Last4,
                    pm.ExpMonth,
                    pm.ExpYear,
                    pm.IsDefault))
                .ToList();

            return Result<IReadOnlyList<TenantSaasPaymentMethodDto>>.Ok(dto);
        }
        catch (Exception ex)
        {
            return Result<IReadOnlyList<TenantSaasPaymentMethodDto>>.Fail(
                Error.Failure("Payments.Provider.Error", ex.Message));
        }
    }
}
