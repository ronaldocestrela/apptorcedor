using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Partner.Queries.LookupByPhone;

public sealed record LookupPartnerByPhoneQuery(string Phone) : IRequest<PartnerLookupResultDto>;

public sealed class LookupPartnerByPhoneQueryHandler(IPartnerLookupPort port)
    : IRequestHandler<LookupPartnerByPhoneQuery, PartnerLookupResultDto>
{
    public Task<PartnerLookupResultDto> Handle(LookupPartnerByPhoneQuery request, CancellationToken cancellationToken)
        => port.LookupByPhoneAsync(request.Phone, cancellationToken);
}
