using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account.Commands.ChangePlan;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class ChangePlanCommandHandlerTests
{
    [Fact]
    public async Task Handler_delegates_to_plan_change_port()
    {
        var userId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();
        var from = new ChangePlanPlanSnapshotDto(Guid.NewGuid(), "A", 50m, "Monthly", 0m);
        var to = new ChangePlanPlanSnapshotDto(newPlanId, "B", 100m, "Monthly", 0m);
        var expected = ChangePlanResult.Success(
            Guid.NewGuid(),
            MembershipStatus.Ativo,
            from,
            to,
            10m,
            Guid.NewGuid(),
            "BRL",
            TorcedorSubscriptionPaymentMethod.Pix,
            new TorcedorSubscriptionCheckoutPixDto("qr", "k"),
            null);

        var port = new FakePort { Result = expected };
        var handler = new ChangePlanCommandHandler(port);

        var r = await handler.Handle(
            new ChangePlanCommand(userId, newPlanId, TorcedorSubscriptionPaymentMethod.Pix),
            CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Equal(expected, r);
        Assert.Single(port.Calls);
        Assert.Equal(userId, port.Calls[0].UserId);
        Assert.Equal(newPlanId, port.Calls[0].NewPlanId);
        Assert.Equal(TorcedorSubscriptionPaymentMethod.Pix, port.Calls[0].Method);
    }

    private sealed class FakePort : ITorcedorPlanChangePort
    {
        public List<(Guid UserId, Guid NewPlanId, TorcedorSubscriptionPaymentMethod Method)> Calls { get; } = [];

        public ChangePlanResult Result { get; init; } = ChangePlanResult.Failure(ChangePlanError.MembershipNotFound);

        public Task<ChangePlanResult> ChangePlanAsync(
            Guid userId,
            Guid newPlanId,
            TorcedorSubscriptionPaymentMethod paymentMethod,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, newPlanId, paymentMethod));
            return Task.FromResult(Result);
        }
    }
}
