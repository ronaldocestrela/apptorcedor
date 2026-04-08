using MediatR;
using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.BuildingBlocks.Application.Abstractions;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}
