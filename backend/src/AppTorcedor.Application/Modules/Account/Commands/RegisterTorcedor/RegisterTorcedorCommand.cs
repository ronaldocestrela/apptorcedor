using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.RegisterTorcedor;

public sealed record RegisterTorcedorCommand(
    string Name,
    string Email,
    string Password,
    string PhoneNumber,
    IReadOnlyList<Guid> AcceptedLegalDocumentVersionIds) : IRequest<RegisterTorcedorResult>;
