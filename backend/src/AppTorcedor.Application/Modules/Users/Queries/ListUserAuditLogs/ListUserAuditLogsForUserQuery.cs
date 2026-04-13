using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Queries.ListUserAuditLogs;

public sealed record ListUserAuditLogsForUserQuery(Guid UserId, int Take = 50) : IRequest<IReadOnlyList<AuditLogRowDto>>;
