using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;
using AppTorcedor.Identity;
using Xunit;

namespace AppTorcedor.Application.Tests;

public sealed class MyDigitalCardViewFactoryTests
{
    [Fact]
    public void NoMembershipRow_is_not_associated_without_ids()
    {
        var dto = MyDigitalCardViewFactory.NoMembershipRow();
        Assert.Equal(MyDigitalCardViewState.NotAssociated, dto.State);
        Assert.Null(dto.MembershipId);
        Assert.Contains("associação", dto.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void NotAssociated_includes_membership_id()
    {
        var id = Guid.NewGuid();
        var dto = MyDigitalCardViewFactory.NotAssociated(id);
        Assert.Equal(MyDigitalCardViewState.NotAssociated, dto.State);
        Assert.Equal(id, dto.MembershipId);
    }

    [Theory]
    [InlineData(MembershipStatus.Inadimplente)]
    [InlineData(MembershipStatus.Suspenso)]
    [InlineData(MembershipStatus.Cancelado)]
    public void InactiveMembership_sets_inactive_state(MembershipStatus status)
    {
        var mid = Guid.NewGuid();
        var dto = MyDigitalCardViewFactory.InactiveMembership(status, mid);
        Assert.Equal(MyDigitalCardViewState.MembershipInactive, dto.State);
        Assert.Equal(status.ToString(), dto.MembershipStatus);
        Assert.Equal(mid, dto.MembershipId);
        Assert.NotNull(dto.Message);
    }

    [Fact]
    public void AwaitingIssuance_has_no_card_payload()
    {
        var mid = Guid.NewGuid();
        var dto = MyDigitalCardViewFactory.AwaitingIssuance(mid);
        Assert.Equal(MyDigitalCardViewState.AwaitingIssuance, dto.State);
        Assert.Null(dto.DigitalCardId);
        Assert.Null(dto.TemplatePreviewLines);
    }

    [Fact]
    public void Active_includes_template_and_cache_hint()
    {
        var mid = Guid.NewGuid();
        var cid = Guid.NewGuid();
        var issued = DateTimeOffset.Parse("2025-01-02T12:00:00Z");
        var dto = MyDigitalCardViewFactory.Active(
            mid,
            cid,
            3,
            issued,
            "ABCDTOKEN",
            "Fulano",
            nameof(MembershipStatus.Ativo),
            "Plano Ouro",
            "***9901",
            nameof(DigitalCardStatus.Active),
            TimeSpan.FromMinutes(5));
        Assert.Equal(MyDigitalCardViewState.Active, dto.State);
        Assert.Equal(cid, dto.DigitalCardId);
        Assert.Equal(3, dto.Version);
        Assert.Equal("ABCDTOKEN", dto.VerificationToken);
        Assert.NotNull(dto.TemplatePreviewLines);
        Assert.Contains(dto.TemplatePreviewLines, l => l.Contains("Fulano", StringComparison.Ordinal));
        Assert.NotNull(dto.CacheValidUntilUtc);
        Assert.True(dto.CacheValidUntilUtc > DateTimeOffset.UtcNow);
    }
}
