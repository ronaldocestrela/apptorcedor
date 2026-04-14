using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMySubscriptionSummary;

public sealed class GetMySubscriptionSummaryQueryHandler(ITorcedorSubscriptionSummaryPort port)
    : IRequestHandler<GetMySubscriptionSummaryQuery, MySubscriptionSummaryDto>
{
    public Task<MySubscriptionSummaryDto> Handle(GetMySubscriptionSummaryQuery request, CancellationToken cancellationToken) =>
        port.GetMySubscriptionSummaryAsync(request.UserId, cancellationToken);
}
