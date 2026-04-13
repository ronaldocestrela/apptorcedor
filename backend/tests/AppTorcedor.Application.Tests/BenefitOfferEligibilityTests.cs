using AppTorcedor.Application.Abstractions;
using AppTorcedor.Identity;

namespace AppTorcedor.Application.Tests;

public sealed class BenefitOfferEligibilityTests
{
    [Fact]
    public void Empty_restrictions_always_match()
    {
        Assert.True(BenefitOfferEligibility.MatchesPlanAndStatus([], [], null));
        Assert.True(
            BenefitOfferEligibility.MatchesPlanAndStatus(
                [],
                [],
                new MembershipRecordSnapshot(Guid.NewGuid(), MembershipStatus.Ativo)));
    }

    [Fact]
    public void Plan_restriction_requires_matching_plan()
    {
        var plan = Guid.NewGuid();
        Assert.False(BenefitOfferEligibility.MatchesPlanAndStatus([plan], [], null));
        Assert.False(
            BenefitOfferEligibility.MatchesPlanAndStatus(
                [plan],
                [],
                new MembershipRecordSnapshot(null, MembershipStatus.Ativo)));
        Assert.True(
            BenefitOfferEligibility.MatchesPlanAndStatus(
                [plan],
                [],
                new MembershipRecordSnapshot(plan, MembershipStatus.Ativo)));
    }

    [Fact]
    public void Status_restriction_requires_matching_status()
    {
        Assert.False(
            BenefitOfferEligibility.MatchesPlanAndStatus(
                [],
                [MembershipStatus.Ativo],
                new MembershipRecordSnapshot(null, MembershipStatus.NaoAssociado)));
        Assert.True(
            BenefitOfferEligibility.MatchesPlanAndStatus(
                [],
                [MembershipStatus.Ativo],
                new MembershipRecordSnapshot(null, MembershipStatus.Ativo)));
    }
}
