using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetMyLoyaltySummary;

public sealed class GetMyLoyaltySummaryQueryHandler(ILoyaltyTorcedorReadPort loyalty)
    : IRequestHandler<GetMyLoyaltySummaryQuery, LoyaltyTorcedorSummaryDto>
{
    public Task<LoyaltyTorcedorSummaryDto> Handle(GetMyLoyaltySummaryQuery request, CancellationToken cancellationToken) =>
        loyalty.GetMySummaryAsync(request.UserId, cancellationToken);
}
