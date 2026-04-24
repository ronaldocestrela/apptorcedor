using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Account;
using MediatR;

namespace AppTorcedor.Application.Modules.Account.Commands.RegisterTorcedor;

public sealed class RegisterTorcedorCommandHandler(ITorcedorAccountPort account)
    : IRequestHandler<RegisterTorcedorCommand, RegisterTorcedorResult>
{
    public Task<RegisterTorcedorResult> Handle(RegisterTorcedorCommand request, CancellationToken cancellationToken)
    {
        var phone = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? string.Empty
            : request.PhoneNumber.Trim();
        var r = new RegisterTorcedorRequest(
            request.Name.Trim(),
            request.Email.Trim(),
            request.Password,
            phone,
            request.AcceptedLegalDocumentVersionIds);
        return account.RegisterAsync(r, cancellationToken);
    }
}
