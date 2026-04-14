using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetDiagnostics;

public sealed record GetDiagnosticsQuery : IRequest<GetDiagnosticsResult>;

public sealed record GetDiagnosticsResult(bool Ok, bool DatabaseConnected);
