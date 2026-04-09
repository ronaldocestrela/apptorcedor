using FluentAssertions;
using SocioTorcedor.BuildingBlocks.Domain.Abstractions;
using SocioTorcedor.Modules.Membership.Domain.Entities;
using SocioTorcedor.Modules.Membership.Domain.Enums;
using SocioTorcedor.Modules.Membership.Domain.Events;
using SocioTorcedor.Modules.Membership.Domain.ValueObjects;

namespace SocioTorcedor.Modules.Membership.Domain.Tests.Entities;

public sealed class MemberProfileTests
{
    private static Cpf ValidCpf() => Cpf.Create("39053344705");

    private static Address ValidAddress() => Address.Create(
        "Rua A",
        "100",
        null,
        "Centro",
        "São Paulo",
        "SP",
        "01310100");

    [Fact]
    public void Create_raises_domain_event_and_sets_active_status()
    {
        var profile = MemberProfile.Create(
            "user-1",
            ValidCpf(),
            new DateTime(1991, 3, 15, 0, 0, 0, DateTimeKind.Utc),
            Gender.Female,
            "11988887777",
            ValidAddress(),
            () => false);

        profile.Status.Should().Be(MemberStatus.Active);
        profile.CpfDigits.Should().Be("39053344705");
        profile.DomainEvents.Should().ContainSingle(e => e is MemberProfileCreatedDomainEvent);
    }

    [Fact]
    public void Create_throws_when_cpf_already_exists()
    {
        var act = () => MemberProfile.Create(
            "user-1",
            ValidCpf(),
            DateTime.UtcNow.AddYears(-20),
            Gender.Male,
            "11999999999",
            ValidAddress(),
            () => true);

        act.Should().Throw<BusinessRuleValidationException>();
    }

    [Fact]
    public void Create_throws_when_user_id_empty()
    {
        var act = () => MemberProfile.Create(
            "  ",
            ValidCpf(),
            DateTime.UtcNow.AddYears(-20),
            Gender.Male,
            "11999999999",
            ValidAddress(),
            () => false);

        act.Should().Throw<ArgumentException>().WithParameterName("userId");
    }

    [Fact]
    public void Update_changes_fields_and_sets_UpdatedAt()
    {
        var profile = MemberProfile.Create(
            "user-1",
            ValidCpf(),
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11888888888",
            ValidAddress(),
            () => false);

        var newAddress = Address.Create(
            "Rua B",
            "200",
            "Bloco 1",
            "Jardim",
            "Campinas",
            "SP",
            "13000000");

        profile.Update(
            new DateTime(1992, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender.Other,
            "11777777777",
            newAddress);

        profile.DateOfBirth.Should().Be(new DateTime(1992, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        profile.Gender.Should().Be(Gender.Other);
        profile.Phone.Should().Be("11777777777");
        profile.Address.City.Should().Be("Campinas");
        profile.UpdatedAt.Should().NotBeNull();
    }
}
