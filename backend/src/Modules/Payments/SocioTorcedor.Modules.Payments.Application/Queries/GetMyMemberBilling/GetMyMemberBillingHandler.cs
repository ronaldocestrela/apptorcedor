using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetMyMemberBilling;

public sealed class GetMyMemberBillingHandler(
    ICurrentUserAccessor currentUserAccessor,
    IMemberProfileRepository memberProfileRepository,
    IMemberPlanRepository memberPlanRepository,
    IMemberTenantPaymentsRepository paymentsRepository)
    : IQueryHandler<GetMyMemberBillingQuery, MemberBillingSubscriptionDto?>
{
    public async Task<Result<MemberBillingSubscriptionDto?>> Handle(
        GetMyMemberBillingQuery query,
        CancellationToken cancellationToken)
    {
        var userId = currentUserAccessor.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return Result<MemberBillingSubscriptionDto?>.Fail(Error.Failure("Auth.Required", "User is not authenticated."));

        var profile = await memberProfileRepository.GetTrackedByUserIdAsync(userId, cancellationToken);
        if (profile is null)
            return Result<MemberBillingSubscriptionDto?>.Ok(null);

        var sub = await paymentsRepository.GetActiveSubscriptionByMemberAsync(profile.Id, cancellationToken);
        if (sub is null)
            return Result<MemberBillingSubscriptionDto?>.Ok(null);

        var plan = await memberPlanRepository.GetByIdAsync(sub.MemberPlanId, cancellationToken);
        var planName = plan?.Nome;

        var dto = new MemberBillingSubscriptionDto(
            sub.Id,
            sub.MemberProfileId,
            sub.MemberPlanId,
            planName,
            sub.RecurringAmount,
            sub.Currency,
            sub.PaymentMethod,
            sub.Status,
            sub.ExternalCustomerId,
            sub.ExternalSubscriptionId,
            sub.NextBillingAtUtc,
            sub.CreatedAtUtc);

        return Result<MemberBillingSubscriptionDto?>.Ok(dto);
    }
}
