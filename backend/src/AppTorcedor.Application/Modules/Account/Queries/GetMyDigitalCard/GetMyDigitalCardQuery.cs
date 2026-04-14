using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Queries.GetMyDigitalCard;

public sealed record GetMyDigitalCardQuery(Guid UserId) : IRequest<MyDigitalCardViewDto>;
