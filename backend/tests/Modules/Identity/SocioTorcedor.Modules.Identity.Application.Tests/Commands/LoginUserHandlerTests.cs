using FluentAssertions;
using NSubstitute;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.Commands.LoginUser;
using SocioTorcedor.Modules.Identity.Application.Contracts;
using SocioTorcedor.Modules.Identity.Application.DTOs;

namespace SocioTorcedor.Modules.Identity.Application.Tests.Commands;

public class LoginUserHandlerTests
{
    [Fact]
    public async Task Delegates_to_identity_service()
    {
        var identity = Substitute.For<IIdentityService>();
        identity.LoginAsync("x@y.com", "secret", Arg.Any<CancellationToken>())
            .Returns(Result<AuthResultDto>.Fail(Error.Failure("Identity.InvalidCredentials", "bad")));

        var handler = new LoginUserHandler(identity);

        var r = await handler.Handle(new LoginUserCommand("x@y.com", "secret"), CancellationToken.None);

        r.IsSuccess.Should().BeFalse();
        await identity.Received(1).LoginAsync("x@y.com", "secret", Arg.Any<CancellationToken>());
    }
}
