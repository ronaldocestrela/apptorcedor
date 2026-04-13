using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetRegistrationLegalRequirements;

public sealed class GetRegistrationLegalRequirementsQueryHandler(IRegistrationLegalReadPort legal)
    : IRequestHandler<GetRegistrationLegalRequirementsQuery, RegistrationLegalRequirementsDto?>
{
    public Task<RegistrationLegalRequirementsDto?> Handle(
        GetRegistrationLegalRequirementsQuery request,
        CancellationToken cancellationToken) =>
        legal.GetRequiredPublishedVersionsAsync(cancellationToken);
}
