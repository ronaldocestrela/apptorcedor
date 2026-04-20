using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Games.Queries.ListOpponentLogoAssets;

public sealed class ListOpponentLogoAssetsQueryHandler(IOpponentLogoLibraryAdminPort library)
    : IRequestHandler<ListOpponentLogoAssetsQuery, OpponentLogoAssetListPageDto>
{
    public Task<OpponentLogoAssetListPageDto> Handle(ListOpponentLogoAssetsQuery request, CancellationToken cancellationToken) =>
        library.ListAsync(request.Page, request.PageSize, cancellationToken);
}
