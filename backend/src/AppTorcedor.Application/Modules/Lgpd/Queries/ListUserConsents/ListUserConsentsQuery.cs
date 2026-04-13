using AppTorcedor.Application.Modules.Lgpd;
using MediatR;

namespace AppTorcedor.Application.Modules.Lgpd.Queries.ListUserConsents;

public sealed record ListUserConsentsQuery(Guid UserId) : IRequest<IReadOnlyList<UserConsentRowDto>>;
