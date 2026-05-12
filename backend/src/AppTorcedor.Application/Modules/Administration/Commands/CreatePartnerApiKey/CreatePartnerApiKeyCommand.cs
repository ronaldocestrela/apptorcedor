using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Commands.CreatePartnerApiKey;

public sealed record CreatePartnerApiKeyCommand(string Name, Guid? CallerUserId) : IRequest<PartnerApiKeyCreatedDto>;

public sealed class CreatePartnerApiKeyCommandHandler(IPartnerApiKeyPort port)
    : IRequestHandler<CreatePartnerApiKeyCommand, PartnerApiKeyCreatedDto>
{
    public Task<PartnerApiKeyCreatedDto> Handle(CreatePartnerApiKeyCommand request, CancellationToken cancellationToken)
        => port.CreateAsync(request.Name, request.CallerUserId, cancellationToken);
}
