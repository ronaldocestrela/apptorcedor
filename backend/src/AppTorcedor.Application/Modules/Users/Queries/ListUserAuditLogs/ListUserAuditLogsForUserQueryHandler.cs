using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Users.Queries.ListUserAuditLogs;

public sealed class ListUserAuditLogsForUserQueryHandler(IAuditLogReadPort auditLogs)
    : IRequestHandler<ListUserAuditLogsForUserQuery, IReadOnlyList<AuditLogRowDto>>
{
    public Task<IReadOnlyList<AuditLogRowDto>> Handle(ListUserAuditLogsForUserQuery request, CancellationToken cancellationToken) =>
        auditLogs.ListForSubjectUserAsync(request.UserId, request.Take, cancellationToken);
}
