using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetRegistrationLegalRequirements;

public sealed record GetRegistrationLegalRequirementsQuery : IRequest<RegistrationLegalRequirementsDto?>;
