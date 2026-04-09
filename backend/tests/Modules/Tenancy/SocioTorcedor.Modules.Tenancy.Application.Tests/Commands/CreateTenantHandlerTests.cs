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
        var generator = Substitute.For<ITenantConnectionStringGenerator>();
        var provisioner = Substitute.For<ITenantDatabaseProvisioner>();
        var handler = new CreateTenantHandler(repo, generator, provisioner);

        var result = await handler.Handle(
            new CreateTenantCommand("Club", "slug"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Tenant.SlugExists");
        await repo.DidNotReceive().AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        generator.DidNotReceive().Generate(Arg.Any<string>());
        await provisioner.DidNotReceive()
            .ProvisionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_tenant_when_slug_free()
    {
        var repo = Substitute.For<ITenantRepository>();
        repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var generator = Substitute.For<ITenantConnectionStringGenerator>();
        const string cs = "Server=.;Database=SocioTorcedor_Tenant_newslug;";
        generator.Generate(Arg.Any<string>()).Returns(cs);
        var provisioner = Substitute.For<ITenantDatabaseProvisioner>();
        var handler = new CreateTenantHandler(repo, generator, provisioner);

        var result = await handler.Handle(
            new CreateTenantCommand("Club", "newslug"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).AddAsync(Arg.Any<Tenant>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await provisioner.Received(1).ProvisionAsync(cs, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_failure_when_provisioning_throws()
    {
        var repo = Substitute.For<ITenantRepository>();
        repo.SlugExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        var generator = Substitute.For<ITenantConnectionStringGenerator>();
        const string cs = "Server=.;Database=SocioTorcedor_Tenant_ab;";
        generator.Generate(Arg.Any<string>()).Returns(cs);
        var provisioner = Substitute.For<ITenantDatabaseProvisioner>();
        provisioner
            .ProvisionAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("migration failed")));

        var handler = new CreateTenantHandler(repo, generator, provisioner);

        var result = await handler.Handle(
            new CreateTenantCommand("Club", "ab"),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Tenant.ProvisioningFailed");
        result.Error.Message.Should().Contain("migration failed");
    }
}
