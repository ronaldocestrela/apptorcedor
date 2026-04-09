using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;
using SocioTorcedor.Modules.Membership.Application.Contracts;
using SocioTorcedor.Modules.Membership.Application.Tests.Support;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Commands;

public sealed class CreateMemberProfileHandlerTests
{
    private static CreateMemberProfileCommand ValidCommand() =>
        new(
            "390.533.447-05",
            new DateTime(1990, 5, 20, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "+5511988887777",
            "Rua das Flores",
            "42",
            "Apto 1",
            "Centro",
            "São Paulo",
            "SP",
            "01310100");

    [Fact]
    public async Task When_tenant_unresolved_returns_Tenant_Required()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");

        var handler = new CreateMemberProfileHandler(repo, user, new UnresolvedTenant());

        var r = await handler.Handle(ValidCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Tenant.Required");
        await repo.DidNotReceive()
            .AddAsync(Arg.Any<MemberProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task When_user_missing_returns_Membership_UserRequired()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns((string?)null);

        var handler = new CreateMemberProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(ValidCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.UserRequired");
    }

    [Fact]
    public async Task When_profile_already_exists_returns_ProfileExists()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.ExistsByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");

        var handler = new CreateMemberProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(ValidCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.ProfileExists");
    }

    [Fact]
    public async Task When_cpf_taken_returns_CpfConflict()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.ExistsByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(false);
        repo.ExistsByCpfDigitsAsync("39053344705", Arg.Any<CancellationToken>()).Returns(true);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");

        var handler = new CreateMemberProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(ValidCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        r.Error!.Code.Should().Be("Membership.CpfConflict");
    }

    [Fact]
    public async Task Persists_profile_and_returns_dto()
    {
        var repo = Substitute.For<IMemberProfileRepository>();
        repo.ExistsByUserIdAsync("u1", Arg.Any<CancellationToken>()).Returns(false);
        repo.ExistsByCpfDigitsAsync("39053344705", Arg.Any<CancellationToken>()).Returns(false);
        var user = Substitute.For<ICurrentUserAccessor>();
        user.GetUserId().Returns("u1");

        var handler = new CreateMemberProfileHandler(repo, user, new ResolvedTenant(Guid.NewGuid()));

        var r = await handler.Handle(ValidCommand(), CancellationToken.None);

        r.IsSuccess.Should().BeTrue();
        r.Value!.UserId.Should().Be("u1");
        r.Value.CpfDigits.Should().Be("39053344705");
        r.Value.Address.City.Should().Be("São Paulo");
        await repo.Received(1).AddAsync(Arg.Any<MemberProfile>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
