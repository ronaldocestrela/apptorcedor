using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListAuditLogs;

public sealed record ListAuditLogsQuery(string? EntityType, int Take = 50) : IRequest<IReadOnlyList<AuditLogRowDto>>;
