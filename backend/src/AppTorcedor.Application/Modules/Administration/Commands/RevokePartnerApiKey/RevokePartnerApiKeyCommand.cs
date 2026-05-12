using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.RevokePartnerApiKey;

public sealed record RevokePartnerApiKeyCommand(Guid Id) : IRequest<bool>;

public sealed class RevokePartnerApiKeyCommandHandler(IPartnerApiKeyPort port)
    : IRequestHandler<RevokePartnerApiKeyCommand, bool>
{
    public Task<bool> Handle(RevokePartnerApiKeyCommand request, CancellationToken cancellationToken)
        => port.RevokeAsync(request.Id, cancellationToken);
}
