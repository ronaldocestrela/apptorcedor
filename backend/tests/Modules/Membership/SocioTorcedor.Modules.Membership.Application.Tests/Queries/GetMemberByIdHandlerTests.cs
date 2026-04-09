using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Queries.GetMemberById;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Queries;

public sealed class GetMemberByIdHandlerTests
{
    private static MemberProfile SampleProfile()
    {
        var cpf = Cpf.Create("39053344705");
        var address = Address.Create("Rua A", "1", null, "B", "City", "SP", "01310100");
        return MemberProfile.Create(
            "other-user",
            cpf,
            new DateTime(1988, 4, 4, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11999999999",
            address,
            () => false);
    }

    [Fact]
    public async Task When_missing_returns_not_found()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        var id = Guid.NewGuid();
        repo.GetTrackedByIdAsync(id, Arg.Any<CancellationToken>()).Returns((MemberProfile?)null);

        var handler = new GetMemberByIdHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new GetMemberByIdQuery(id), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.ProfileNotFound");
    }

    [Fact]
    public async Task Returns_profile()
    {
        var profile = SampleProfile();
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.GetTrackedByIdAsync(profile.Id, Arg.Any<CancellationToken>()).Returns(profile);

        var handler = new GetMemberByIdHandler(repo, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(new GetMemberByIdQuery(profile.Id), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.Id.Should().Be(profile.Id);
    }
}
