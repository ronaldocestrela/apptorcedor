using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Queries.GetStripeConnectStatus;

public sealed class GetStripeConnectStatusHandler(ITenantMasterPaymentsRepository paymentsRepository)
    : IQueryHandler<GetStripeConnectStatusQuery, StripeConnectStatusDto>
{
    public async Task<Result<StripeConnectStatusDto>> Handle(
        GetStripeConnectStatusQuery query,
        CancellationToken cancellationToken)
    {
        var row = await paymentsRepository.GetStripeConnectByTenantIdAsync(query.TenantId, cancellationToken);
        if (row is null)
        {
            return Result<StripeConnectStatusDto>.Ok(
                new StripeConnectStatusDto(false, null, 0, false, false, false));
        }

        return Result<StripeConnectStatusDto>.Ok(
            new StripeConnectStatusDto(
                true,
                row.StripeAccountId,
                (int)row.OnboardingStatus,
                row.ChargesEnabled,
                row.PayoutsEnabled,
                row.DetailsSubmitted));
    }
}
