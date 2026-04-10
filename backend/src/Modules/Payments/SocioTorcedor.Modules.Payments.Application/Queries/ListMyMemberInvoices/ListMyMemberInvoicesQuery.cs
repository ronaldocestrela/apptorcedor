using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.ListMyMemberInvoices;

public sealed record ListMyMemberInvoicesQuery(int Page, int PageSize)
    : IQuery<PagedResult<MemberBillingInvoiceDto>>;
