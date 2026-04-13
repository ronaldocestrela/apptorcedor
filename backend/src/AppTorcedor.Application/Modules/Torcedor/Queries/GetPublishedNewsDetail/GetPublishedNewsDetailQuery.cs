using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Torcedor.Queries.GetPublishedNewsDetail;

public sealed record GetPublishedNewsDetailQuery(Guid NewsId) : IRequest<TorcedorNewsDetailDto?>;
