using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Queries.ListUserConsents;

public sealed class ListUserConsentsQueryHandler(ILgpdAdministrationPort lgpd)
    : IRequestHandler<ListUserConsentsQuery, IReadOnlyList<UserConsentRowDto>>
{
    public Task<IReadOnlyList<UserConsentRowDto>> Handle(ListUserConsentsQuery request, CancellationToken cancellationToken) =>
        lgpd.ListConsentsForUserAsync(request.UserId, cancellationToken);
}
