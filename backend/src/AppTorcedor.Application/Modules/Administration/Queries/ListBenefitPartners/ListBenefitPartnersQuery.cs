using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListBenefitPartners;

public sealed record ListBenefitPartnersQuery(string? Search, bool? IsActive, int Page, int PageSize)
    : IRequest<BenefitPartnerListPageDto>;
