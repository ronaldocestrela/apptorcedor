using FluentAssertions;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SocioTorcedor.BuildingBlocks.Application;
using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.BuildingBlocks.Application.Tests.Behaviors;

public class ValidationBehaviorTests
{
    public sealed record PingCommand(string Name) : ICommand;

    public sealed class PingCommandValidator : AbstractValidator<PingCommand>
    {
        public PingCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    public sealed class PingCommandHandler : ICommandHandler<PingCommand>
    {
        public Task<Result> Handle(PingCommand command, CancellationToken cancellationToken)
            => Task.FromResult(Result.Ok());
    }

    public sealed record EchoQuery(string Text) : IQuery<string>;

    public sealed class EchoQueryValidator : AbstractValidator<EchoQuery>
    {
        public EchoQueryValidator()
        {
            RuleFor(x => x.Text).MinimumLength(3);
        }
    }

    public sealed class EchoQueryHandler : IQueryHandler<EchoQuery, string>
    {
        public Task<Result<string>> Handle(EchoQuery query, CancellationToken cancellationToken)
            => Task.FromResult(Result<string>.Ok(query.Text));
    }

    private static IServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddBuildingBlocksApplication(typeof(ValidationBehaviorTests).Assembly);
        return services.BuildServiceProvider();
    }

    [Fact]
    public async Task Invalid_command_returns_failed_result_without_calling_handler()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PingCommand(string.Empty));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be(nameof(PingCommand.Name));
    }

    [Fact]
    public async Task Valid_command_reaches_handler()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new PingCommand("ok"));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Invalid_query_returns_failed_Result_of_T()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoQuery("ab"));

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeNull();
    }

    [Fact]
    public async Task Valid_query_returns_value()
    {
        var provider = BuildServices();
        var mediator = provider.GetRequiredService<IMediator>();

        var result = await mediator.Send(new EchoQuery("hello"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be("hello");
    }
}
