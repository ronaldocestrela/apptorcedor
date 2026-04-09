using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Tenancy.Application.Contracts;
using SocioTorcedor.Modules.Tenancy.Application.DTOs;
using SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantBySlug;

namespace SocioTorcedor.Modules.Tenancy.Application.Tests.Queries;

public class GetTenantBySlugHandlerTests
{
    [Fact]
    public async Task Returns_failure_when_slug_empty()
    {
        var repo = Substitute.For<ITenantRepository>();
        var handler = new GetTenantBySlugHandler(repo);

        var result = await handler.Handle(new GetTenantBySlugQuery("  "), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await repo.DidNotReceive().GetBySlugAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Returns_not_found_when_repository_returns_null()
    {
        var repo = Substitute.For<ITenantRepository>();
        repo.GetBySlugAsync("x", Arg.Any<CancellationToken>()).Returns((TenantDto?)null);
        var handler = new GetTenantBySlugHandler(repo);

        var result = await handler.Handle(new GetTenantBySlugQuery("x"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error!.Code.Should().Be("Tenant.NotFound");
    }

    [Fact]
    public async Task Returns_context_when_found()
    {
        var dto = new TenantDto(
            Guid.NewGuid(),
            "Club",
            "ffc",
            "cs",
            new[] { "https://ffc.app" });
        var repo = Substitute.For<ITenantRepository>();
        repo.GetBySlugAsync("ffc", Arg.Any<CancellationToken>()).Returns(dto);
        var handler = new GetTenantBySlugHandler(repo);

        var result = await handler.Handle(new GetTenantBySlugQuery("ffc"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TenantId.Should().Be(dto.TenantId);
        result.Value.AllowedOrigins.Should().Contain("https://ffc.app");
    }
}
