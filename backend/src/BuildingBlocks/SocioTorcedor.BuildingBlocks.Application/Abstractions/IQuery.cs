using MediatR;
using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.BuildingBlocks.Application.Abstractions;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}
