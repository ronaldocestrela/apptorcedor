using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListBenefitRedemptions;

public sealed record ListBenefitRedemptionsQuery(
    Guid? OfferId,
    Guid? UserId,
    BenefitRedemptionStatus? Status,
    int Page,
    int PageSize)
    : IRequest<BenefitRedemptionListPageDto>;
