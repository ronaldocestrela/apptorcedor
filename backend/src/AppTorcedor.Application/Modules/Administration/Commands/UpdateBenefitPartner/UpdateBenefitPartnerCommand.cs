using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.UpdateBenefitPartner;

public sealed record UpdateBenefitPartnerCommand(Guid PartnerId, BenefitPartnerWriteDto Dto) : IRequest<BenefitMutationResult>;
