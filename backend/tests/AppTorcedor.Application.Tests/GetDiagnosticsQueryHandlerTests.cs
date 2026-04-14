using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Queries.GetDiagnostics;

namespace AppTorcedor.Application.Tests;

public sealed class GetDiagnosticsQueryHandlerTests
{
    [Fact]
    public async Task Returns_database_flag_from_port()
    {
        var handler = new GetDiagnosticsQueryHandler(new FakeDbCheck(true));
        var result = await handler.Handle(new GetDiagnosticsQuery(), CancellationToken.None);
        Assert.True(result.Ok);
        Assert.True(result.DatabaseConnected);

        var handler2 = new GetDiagnosticsQueryHandler(new FakeDbCheck(false));
        var result2 = await handler2.Handle(new GetDiagnosticsQuery(), CancellationToken.None);
        Assert.True(result2.Ok);
        Assert.False(result2.DatabaseConnected);
    }

    private sealed class FakeDbCheck(bool canConnect) : IDatabaseConnectivityCheck
    {
        public Task<bool> CanConnectAsync(CancellationToken cancellationToken = default) => Task.FromResult(canConnect);
    }
}
