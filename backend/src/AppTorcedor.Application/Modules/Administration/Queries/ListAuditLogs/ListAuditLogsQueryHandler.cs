using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAuditLogs;

public sealed class ListAuditLogsQueryHandler(IAuditLogReadPort auditLogs)
    : IRequestHandler<ListAuditLogsQuery, IReadOnlyList<AuditLogRowDto>>
{
    public Task<IReadOnlyList<AuditLogRowDto>> Handle(ListAuditLogsQuery request, CancellationToken cancellationToken) =>
        auditLogs.ListRecentAsync(request.EntityType, request.Take, cancellationToken);
}
