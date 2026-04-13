using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account.Commands.CancelMembership;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class CancelMembershipCommandHandlerTests
{
    [Fact]
    public async Task Handler_delegates_to_cancellation_port()
    {
        var userId = Guid.NewGuid();
        var mid = Guid.NewGuid();
        var until = DateTimeOffset.Parse("2025-06-01T12:00:00Z");
        var expected = CancelMembershipResult.Success(
            mid,
            MembershipStatus.Cancelado,
            TorcedorMembershipCancellationMode.Immediate,
            until,
            "Assinatura cancelada.");

        var port = new FakePort { Result = expected };
        var handler = new CancelMembershipCommandHandler(port);

        var r = await handler.Handle(new CancelMembershipCommand(userId), CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Equal(expected, r);
        Assert.Single(port.Calls);
        Assert.Equal(userId, port.Calls[0]);
    }

    private sealed class FakePort : ITorcedorMembershipCancellationPort
    {
        public List<Guid> Calls { get; } = [];

        public CancelMembershipResult Result { get; init; } = CancelMembershipResult.Failure(CancelMembershipError.MembershipNotFound);

        public Task<CancelMembershipResult> CancelMembershipAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            Calls.Add(userId);
            return Task.FromResult(Result);
        }
    }
}
