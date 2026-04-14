using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Commands.SubscribeMember;

public sealed class SubscribeMemberCommandHandler(
    ITorcedorMembershipSubscriptionPort subscription,
    IPublisher publisher) : IRequestHandler<SubscribeMemberCommand, SubscribeMemberResult>
{
    public async Task<SubscribeMemberResult> Handle(SubscribeMemberCommand request, CancellationToken cancellationToken)
    {
        var result = await subscription
            .SubscribeToPlanAsync(request.UserId, request.PlanId, cancellationToken)
            .ConfigureAwait(false);

        if (result is { Ok: true, MembershipId: { } mid, PlanId: { } pid, UserId: { } uid, Status: { } st })
        {
            await publisher
                .Publish(
                    new MemberSubscribedEvent(mid, uid, pid, st, DateTimeOffset.UtcNow),
                    cancellationToken)
                .ConfigureAwait(false);
        }

        return result;
    }
}
