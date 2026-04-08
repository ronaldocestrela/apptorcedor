using MediatR;
using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.BuildingBlocks.Application.Abstractions;

public interface ICommandHandler<in TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

public interface ICommandHandler<in TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}
