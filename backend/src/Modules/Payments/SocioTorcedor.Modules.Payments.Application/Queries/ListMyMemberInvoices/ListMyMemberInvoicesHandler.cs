using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.ListMyMemberInvoices;

public sealed class ListMyMemberInvoicesHandler(
    ICurrentUserAccessor currentUserAccessor,
    IMemberProfileRepository memberProfileRepository,
    IMemberTenantPaymentsRepository paymentsRepository)
    : IQueryHandler<ListMyMemberInvoicesQuery, PagedResult<MemberBillingInvoiceDto>>
{
    public async Task<Result<PagedResult<MemberBillingInvoiceDto>>> Handle(
        ListMyMemberInvoicesQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Result<PagedResult<MemberBillingInvoiceDto>>.Fail(Error.Failure("Auth.Required", "User is not authenticated."));

        var profile = await memberProfileRepository.GetTrackedByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<PagedResult<MemberBillingInvoiceDto>>.Fail(Error.NotFound("Member.NotFound", "Member profile not found."));

        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);
        var skip = (page - 1) * pageSize;

        var total = await paymentsRepository.CountInvoicesByMemberAsync(profile.Id, cancellationToken);
        var items = await paymentsRepository.ListInvoicesByMemberAsync(profile.Id, skip, pageSize, cancellationToken);

        var dtos = items
            .Select(i => new MemberBillingInvoiceDto(
                i.Id,
                i.MemberBillingSubscriptionId,
                i.Amount,
                i.Currency,
                i.PaymentMethod,
                i.DueAtUtc,
                i.Status,
                i.ExternalInvoiceId,
                i.PixCopyPaste,
                i.PaidAtUtc,
                i.CreatedAtUtc))
            .ToList();

        return Result<PagedResult<MemberBillingInvoiceDto>>.Ok(
            new PagedResult<MemberBillingInvoiceDto>(dtos, total, page, pageSize));
    }
}
