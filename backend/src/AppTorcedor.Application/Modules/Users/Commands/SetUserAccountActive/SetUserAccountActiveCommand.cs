using MediatR;

namespace AppTorcedor.Application.Modules.Users.Commands.SetUserAccountActive;

public sealed record SetUserAccountActiveCommand(Guid UserId, bool IsActive) : IRequest<bool>;
