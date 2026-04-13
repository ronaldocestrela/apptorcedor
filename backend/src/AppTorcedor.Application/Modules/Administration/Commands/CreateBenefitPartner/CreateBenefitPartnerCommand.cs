using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitPartner;

public sealed record CreateBenefitPartnerCommand(BenefitPartnerWriteDto Dto) : IRequest<BenefitCreateResult>;
