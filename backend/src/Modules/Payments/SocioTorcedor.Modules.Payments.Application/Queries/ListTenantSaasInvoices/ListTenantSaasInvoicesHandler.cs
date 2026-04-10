using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.ListTenantSaasInvoices;

public sealed class ListTenantSaasInvoicesHandler(ITenantMasterPaymentsRepository repository)
    : IQueryHandler<ListTenantSaasInvoicesQuery, PagedResult<TenantSaasBillingInvoiceDto>>
{
    public async Task<Result<PagedResult<TenantSaasBillingInvoiceDto>>> Handle(
        ListTenantSaasInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var total = await repository.CountInvoicesByTenantAsync(query.TenantId, cancellationToken);
        var items = await repository.ListInvoicesByTenantAsync(query.TenantId, skip, pageSize, cancellationToken);

        var dtos = items
            .Select(i => new TenantSaasBillingInvoiceDto(
                i.Id,
                i.TenantBillingSubscriptionId,
                i.Amount,
                i.Currency,
                i.DueAtUtc,
                i.Status,
                i.ExternalInvoiceId,
                i.PaidAtUtc,
                i.CreatedAtUtc))
            .ToList();

        return Result<PagedResult<TenantSaasBillingInvoiceDto>>.Ok(
            new PagedResult<TenantSaasBillingInvoiceDto>(dtos, total, page, pageSize));
    }
}
