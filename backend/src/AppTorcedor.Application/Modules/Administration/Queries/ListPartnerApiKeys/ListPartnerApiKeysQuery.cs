using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.ListPartnerApiKeys;

public sealed record ListPartnerApiKeysQuery : IRequest<IReadOnlyList<PartnerApiKeyListItemDto>>;

public sealed class ListPartnerApiKeysQueryHandler(IPartnerApiKeyPort port)
    : IRequestHandler<ListPartnerApiKeysQuery, IReadOnlyList<PartnerApiKeyListItemDto>>
{
    public Task<IReadOnlyList<PartnerApiKeyListItemDto>> Handle(ListPartnerApiKeysQuery request, CancellationToken cancellationToken)
        => port.ListAsync(cancellationToken);
}
