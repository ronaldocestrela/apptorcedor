using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMySubscriptionSummary;

public sealed record GetMySubscriptionSummaryQuery(Guid UserId) : IRequest<MySubscriptionSummaryDto>;
