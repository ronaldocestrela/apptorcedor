using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Tenancy.Application.Commands.CreateTenant;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Domain.Entities;

namespace SocioTorcedor.Modules.Tenancy.Application.Tests.Commands;

public sealed class CreateTenantHandlerTests
{
    [Fact]
    public async Task Returns_conflict_when_slug_exists()
    {
        var repo = Substitute.For<ITenantRepository>();
        repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = new CreateTenantHandler(repo);

        var result = await handler.Handle(
            new CreateTenantCommand("Club", "slug", "Server=.;"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Tenant.SlugExists");
        await repo.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_tenant_when_slug_free()
    {
        var repo = Substitute.For<ITenantRepository>();
        repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = new CreateTenantHandler(repo);

        var result = await handler.Handle(
            new CreateTenantCommand("Club", "newslug", "Server=.;"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
