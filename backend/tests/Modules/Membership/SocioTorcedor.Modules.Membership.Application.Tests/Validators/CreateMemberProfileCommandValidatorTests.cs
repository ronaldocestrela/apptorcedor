using FluentAssertions;
using SocioTorcedor.Modules.Membership.Application.Commands.CreateMemberProfile;
using SocioTorcedor.Modules.Membership.Domain.Enums;

namespace SocioTorcedor.Modules.Membership.Application.Tests.Validators;

public sealed class CreateMemberProfileCommandValidatorTests
{
    private readonly CreateMemberProfileCommandValidator _validator = new();

    private static CreateMemberProfileCommand BaseCommand() =>
        new(
            "39053344705",
            new DateTime(1990, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            Gender.Male,
            "11999999999",
            "Rua",
            "1",
            null,
            "B",
            "C",
            "SP",
            "01310100");

    [Fact]
    public void Valid_command_passes()
    {
        var r = _validator.Validate(BaseCommand());
        r.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_future_date_of_birth()
    {
        var cmd = BaseCommand() with { DateOfBirth = DateTime.UtcNow.Date.AddDays(1) };
        var r = _validator.Validate(cmd);
        r.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Rejects_invalid_cpf()
    {
        var cmd = BaseCommand() with { Cpf = "11111111111" };
        var r = _validator.Validate(cmd);
        r.IsValid.Should().BeFalse();
    }
}
