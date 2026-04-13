using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetDiagnostics;

public sealed class GetDiagnosticsQueryHandler(IDatabaseConnectivityCheck database)
    : IRequestHandler<GetDiagnosticsQuery, GetDiagnosticsResult>
{
    public async Task<GetDiagnosticsResult> Handle(GetDiagnosticsQuery request, CancellationToken cancellationToken)
    {
        var dbOk = await database.CanConnectAsync(cancellationToken).ConfigureAwait(false);
        return new GetDiagnosticsResult(Ok: true, DatabaseConnected: dbOk);
    }
}
