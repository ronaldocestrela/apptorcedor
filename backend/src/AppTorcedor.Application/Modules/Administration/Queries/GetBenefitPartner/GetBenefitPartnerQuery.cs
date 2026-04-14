using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetBenefitPartner;

public sealed record GetBenefitPartnerQuery(Guid PartnerId) : IRequest<BenefitPartnerDetailDto?>;
