using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Games.Queries.ListOpponentLogoAssets;

public sealed record ListOpponentLogoAssetsQuery(int Page, int PageSize) : IRequest<OpponentLogoAssetListPageDto>;
