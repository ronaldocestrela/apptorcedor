using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;

public sealed class GetMyDigitalCardQueryHandler(IDigitalCardTorcedorPort port)
    : IRequestHandler<GetMyDigitalCardQuery, MyDigitalCardViewDto>
{
    public Task<MyDigitalCardViewDto> Handle(GetMyDigitalCardQuery request, CancellationToken cancellationToken) =>
        port.GetMyDigitalCardAsync(request.UserId, cancellationToken);
}
