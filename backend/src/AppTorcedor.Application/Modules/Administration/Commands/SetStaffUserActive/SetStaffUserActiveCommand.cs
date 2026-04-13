using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.SetStaffUserActive;

public sealed record SetStaffUserActiveCommand(Guid UserId, bool IsActive) : IRequest<bool>;
