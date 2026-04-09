using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Commands.UpdateMemberProfile;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Commands;

public sealed class UpdateMemberProfileHandlerTests
{
    private static MemberProfile ExistingProfile(string userId = "u1")
    {
        var cpf = Cpf.Create("39053344705");
        var address = Address.Create("Rua A", "1", null, "B", "City", "SP", "01310100");
        return MemberProfile.Create(
            userId,
            cpf,
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11999999999",
            address,
            () => false);
    }

    private static UpdateMemberProfileCommand ValidUpdateCommand() =>
        new(
            new DateTime(1991, 2, 2, 0, 0, 0, DateTimeKind.Utc),
            Gender.Female,
            "11888888888",
            "Rua Nova",
            "200",
            null,
            "Jardim",
            "Campinas",
            "SP",
            "13000000");

    [Fact]
    public async Task When_profile_missing_returns_not_found()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns((MemberProfile?)null);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");

        var handler = new UpdateMemberProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(ValidUpdateCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.ProfileNotFound");
    }

    [Fact]
    public async Task Updates_and_saves()
    {
        var profile = ExistingProfile();
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(profile);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");

        var handler = new UpdateMemberProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(ValidUpdateCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Phone.Should().Be("11888888888");
        r.Value.Address.City.Should().Be("Campinas");
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
