using FluentAssertions;
using NSubstitute;
using SocioTorcedor.Modules.Backoffice.Application.Commands.CreateSaaSPlan;
using SocioTorcedor.Modules.Backoffice.Application.Contracts;
using SocioTorcedor.Modules.Backoffice.Domain.Entities;

namespace SocioTorcedor.Modules.Backoffice.Application.Tests.Commands;

public sealed class CreateSaaSPlanHandlerTests
{
    [Fact]
    public async Task Persists_plan()
    {
        var repo = Substitute.For<ISaaSPlanRepository>();
        var handler = new CreateSaaSPlanHandler(repo);

        var result = await handler.Handle(
            new CreateSaaSPlanCommand("Pro", null, 9.99m, 99m, 1000, null, null, null),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await repo.Received(1).AddAsync(Arg.Any<SaaSPlan>(), Arg.Any<CancellationToken>());
        await repo.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
