using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMyProfile;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Queries;

public sealed class GetMyProfileHandlerTests
{
    private static MemberProfile SampleProfile()
    {
        var cpf = Cpf.Create("39053344705");
        var address = Address.Create("Rua A", "1", null, "B", "City", "SP", "01310100");
        return MemberProfile.Create(
            "me-user",
            cpf,
            new DateTime(1988, 4, 4, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11999999999",
            address,
            () => false);
    }

    [Fact]
    public async Task When_not_found_returns_error()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByUserIdAsync("me-user", Arg.Any<CancellationToken>()).Returns((MemberProfile?)null);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("me-user");

        var handler = new GetMyProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.ProfileNotFound");
    }

    [Fact]
    public async Task Returns_dto_when_found()
    {
        var p = SampleProfile();
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByUserIdAsync("me-user", Arg.Any<CancellationToken>()).Returns(p);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("me-user");

        var handler = new GetMyProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Id.Should().Be(p.Id);
        r.Value.CpfDigits.Should().Be("39053344705");
    }
}
