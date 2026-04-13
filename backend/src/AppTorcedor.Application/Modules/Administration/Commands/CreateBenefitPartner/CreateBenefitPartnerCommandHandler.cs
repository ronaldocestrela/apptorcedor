using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreateBenefitPartner;

public sealed class CreateBenefitPartnerCommandHandler(IBenefitsAdministrationPort benefits)
    : IRequestHandler<CreateBenefitPartnerCommand, BenefitCreateResult>
{
    public Task<BenefitCreateResult> Handle(CreateBenefitPartnerCommand request, CancellationToken cancellationToken) =>
        benefits.CreatePartnerAsync(request.Dto, cancellationToken);
}
