using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account.Queries.GetMySubscriptionSummary;
using AppTorcedor.Identity;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class GetMySubscriptionSummaryQueryHandlerTests
{
    [Fact]
    public async Task Handler_delegates_to_subscription_summary_port()
    {
        var uid = Guid.NewGuid();
        var expected = new MySubscriptionSummaryDto(
            HasMembership: false,
            MembershipId: null,
            MembershipStatus: null,
            StartDate: null,
            EndDate: null,
            NextDueDate: null,
            Plan: null,
            LastPayment: null,
            DigitalCard: null);
        var fake = new FakePort { Result = expected };
        var handler = new GetMySubscriptionSummaryQueryHandler(fake);
        var r = await handler.Handle(new GetMySubscriptionSummaryQuery(uid), CancellationToken.None);
        Assert.Same(expected, r);
        Assert.Single(fake.Calls);
        Assert.Equal(uid, fake.Calls[0]);
    }

    [Fact]
    public async Task Handler_passes_user_id_for_membership_payload()
    {
        var uid = Guid.NewGuid();
        var expected = new MySubscriptionSummaryDto(
            HasMembership: true,
            MembershipId: Guid.NewGuid(),
            MembershipStatus: "Ativo",
            StartDate: DateTimeOffset.UtcNow,
            EndDate: null,
            NextDueDate: DateTimeOffset.UtcNow.AddMonths(1),
            Plan: new MySubscriptionSummaryPlanDto(
                Guid.NewGuid(),
                "Plano X",
                99m,
                "Monthly",
                0m),
            LastPayment: new MySubscriptionSummaryPaymentDto(
                Guid.NewGuid(),
                99m,
                "BRL",
                "Paid",
                "Pix",
                DateTimeOffset.UtcNow,
                DateTimeOffset.UtcNow),
            DigitalCard: new MySubscriptionSummaryDigitalCardDto(
                MyDigitalCardViewState.AwaitingIssuance,
                nameof(MembershipStatus.Ativo),
                "msg"));
        var fake = new FakePort { Result = expected };
        var handler = new GetMySubscriptionSummaryQueryHandler(fake);
        var r = await handler.Handle(new GetMySubscriptionSummaryQuery(uid), CancellationToken.None);
        Assert.Equal(expected, r);
        Assert.Equal(uid, fake.Calls[0]);
    }

    private sealed class FakePort : ITorcedorSubscriptionSummaryPort
    {
        public MySubscriptionSummaryDto Result { get; init; } = new(
            false,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null);

        public List<Guid> Calls { get; } = [];

        public Task<MySubscriptionSummaryDto> GetMySubscriptionSummaryAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            Calls.Add(userId);
            return Task.FromResult(Result);
        }
    }
}
