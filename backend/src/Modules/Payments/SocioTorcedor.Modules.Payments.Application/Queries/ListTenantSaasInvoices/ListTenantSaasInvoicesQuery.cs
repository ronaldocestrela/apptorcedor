using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasInvoices;

public sealed record ListTenantSaasInvoicesQuery(Guid TenantId, int Page, int PageSize)
    : IQuery<PagedResult<TenantSaasBillingInvoiceDto>>;
