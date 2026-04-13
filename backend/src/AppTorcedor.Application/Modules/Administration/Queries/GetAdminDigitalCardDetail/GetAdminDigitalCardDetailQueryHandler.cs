using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminDigitalCardDetail;

public sealed class GetAdminDigitalCardDetailQueryHandler(IDigitalCardAdministrationPort port)
    : IRequestHandler<GetAdminDigitalCardDetailQuery, AdminDigitalCardDetailDto?>
{
    public Task<AdminDigitalCardDetailDto?> Handle(GetAdminDigitalCardDetailQuery request, CancellationToken cancellationToken) =>
        port.GetDigitalCardByIdAsync(request.DigitalCardId, cancellationToken);
}
