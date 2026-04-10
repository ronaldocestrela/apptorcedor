using System.Reflection;
using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Membership.Application.Commands.ChangeMemberStatus;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Commands;

public sealed class ChangeMemberStatusHandlerTests
{
    private static MemberProfile SampleProfile(Guid id)
    {
        var p = MemberProfile.Create(
            "user-1",
            Cpf.Create("39053344705"),
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11888888888",
            Address.Create("Rua A", "1", null, "Centro", "SP City", "SP", "01310100"),
            () => false);
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(p, id);
        return p;
    }

    [Fact]
    public async Task When_profile_missing_returns_not_found()
    {
        var id = Guid.NewGuid();
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns((MemberProfile?)null);

        var handler = new ChangeMemberStatusHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ChangeMemberStatusCommand(id, MemberStatus.Suspended), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.ProfileNotFound");
    }

    [Fact]
    public async Task When_transition_invalid_returns_validation_error()
    {
        var id = Guid.NewGuid();
        var profile = SampleProfile(id);
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = new ChangeMemberStatusHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ChangeMemberStatusCommand(id, MemberStatus.PendingCompletion), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.InvalidStatusTransition");
    }

    [Fact]
    public async Task Persists_valid_transition()
    {
        var id = Guid.NewGuid();
        var profile = SampleProfile(id);
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = new ChangeMemberStatusHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new ChangeMemberStatusCommand(id, MemberStatus.Suspended), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        profile.Status.Should().Be(MemberStatus.Suspended);
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
