using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListDigitalCardIssueCandidates;

public sealed class ListDigitalCardIssueCandidatesQueryHandler(IDigitalCardAdministrationPort port)
    : IRequestHandler<ListDigitalCardIssueCandidatesQuery, AdminDigitalCardIssueCandidatesPageDto>
{
    public Task<AdminDigitalCardIssueCandidatesPageDto> Handle(
        ListDigitalCardIssueCandidatesQuery request,
        CancellationToken cancellationToken) =>
        port.ListDigitalCardIssueCandidatesAsync(request.Page, request.PageSize, cancellationToken);
}
