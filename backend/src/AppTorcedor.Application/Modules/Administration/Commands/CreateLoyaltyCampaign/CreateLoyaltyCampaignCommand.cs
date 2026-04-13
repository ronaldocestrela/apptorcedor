using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateLoyaltyCampaign;

public sealed record CreateLoyaltyCampaignCommand(LoyaltyCampaignWriteDto Dto) : IRequest<LoyaltyCampaignCreateResult>;
