using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasPaymentMethods;

public sealed record ListTenantSaasPaymentMethodsQuery(Guid TenantId)
    : IQuery<IReadOnlyList<TenantSaasPaymentMethodDto>>;
