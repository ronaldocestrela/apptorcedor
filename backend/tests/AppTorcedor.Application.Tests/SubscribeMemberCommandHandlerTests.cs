using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Torcedor.Commands.SubscribeMember;
using AppTorcedor.Identity;
using MediatR;

namespace AppTorcedor.Application.Tests;

public sealed class SubscribeMemberCommandHandlerTests
{
    [Fact]
    public async Task Handle_delegates_to_subscription_port()
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var fakeSub = new FakeTorcedorMembershipSubscriptionPort
        {
            Result = SubscribeMemberResult.Success(membershipId, userId, planId, MembershipStatus.PendingPayment),
        };
        var fakePub = new CapturingPublisher();
        var handler = new SubscribeMemberCommandHandler(fakeSub, fakePub);

        var result = await handler.Handle(new SubscribeMemberCommand(userId, planId), CancellationToken.None);

        Assert.True(result.Ok);
        Assert.Single(fakeSub.Calls);
        Assert.Equal(userId, fakeSub.Calls[0].UserId);
        Assert.Equal(planId, fakeSub.Calls[0].PlanId);
    }

    [Fact]
    public async Task Handle_publishes_MemberSubscribedEvent_on_success()
    {
        var userId = Guid.NewGuid();
        var planId = Guid.NewGuid();
        var membershipId = Guid.NewGuid();
        var fakeSub = new FakeTorcedorMembershipSubscriptionPort
        {
            Result = SubscribeMemberResult.Success(membershipId, userId, planId, MembershipStatus.PendingPayment),
        };
        var fakePub = new CapturingPublisher();
        var handler = new SubscribeMemberCommandHandler(fakeSub, fakePub);

        await handler.Handle(new SubscribeMemberCommand(userId, planId), CancellationToken.None);

        Assert.Single(fakePub.Notifications);
        var n = Assert.IsType<MemberSubscribedEvent>(fakePub.Notifications[0]);
        Assert.Equal(membershipId, n.MembershipId);
        Assert.Equal(userId, n.UserId);
        Assert.Equal(planId, n.PlanId);
        Assert.Equal(MembershipStatus.PendingPayment, n.Status);
    }

    [Fact]
    public async Task Handle_does_not_publish_on_failure()
    {
        var fakeSub = new FakeTorcedorMembershipSubscriptionPort
        {
            Result = SubscribeMemberResult.Failure(SubscribeMemberError.PlanNotFoundOrNotAvailable),
        };
        var fakePub = new CapturingPublisher();
        var handler = new SubscribeMemberCommandHandler(fakeSub, fakePub);

        var result = await handler.Handle(new SubscribeMemberCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Ok);
        Assert.Equal(SubscribeMemberError.PlanNotFoundOrNotAvailable, result.Error);
        Assert.Empty(fakePub.Notifications);
    }

    [Fact]
    public async Task Handle_returns_port_failure_for_active_member()
    {
        var fakeSub = new FakeTorcedorMembershipSubscriptionPort
        {
            Result = SubscribeMemberResult.Failure(SubscribeMemberError.AlreadyActiveSubscription),
        };
        var fakePub = new CapturingPublisher();
        var handler = new SubscribeMemberCommandHandler(fakeSub, fakePub);

        var result = await handler.Handle(new SubscribeMemberCommand(Guid.NewGuid(), Guid.NewGuid()), CancellationToken.None);

        Assert.False(result.Ok);
        Assert.Equal(SubscribeMemberError.AlreadyActiveSubscription, result.Error);
        Assert.Empty(fakePub.Notifications);
    }

    private sealed class FakeTorcedorMembershipSubscriptionPort : ITorcedorMembershipSubscriptionPort
    {
        public List<(Guid UserId, Guid PlanId)> Calls { get; } = [];

        public SubscribeMemberResult Result { get; init; } =
            SubscribeMemberResult.Failure(SubscribeMemberError.PlanNotFoundOrNotAvailable);

        public Task<SubscribeMemberResult> SubscribeToPlanAsync(
            Guid userId,
            Guid planId,
            CancellationToken cancellationToken = default)
        {
            Calls.Add((userId, planId));
            return Task.FromResult(Result);
        }
    }

    private sealed class CapturingPublisher : IPublisher
    {
        public List<object> Notifications { get; } = [];

        public Task Publish(object notification, CancellationToken cancellationToken = default)
        {
            Notifications.Add(notification);
            return Task.CompletedTask;
        }

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification {
            Notifications.Add(notification!);
            return Task.CompletedTask;
        }
    }
}
