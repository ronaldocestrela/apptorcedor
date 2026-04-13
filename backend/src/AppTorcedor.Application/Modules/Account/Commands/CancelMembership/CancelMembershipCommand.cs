using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.CancelMembership;

public sealed record CancelMembershipCommand(Guid UserId) : IRequest<CancelMembershipResult>;
