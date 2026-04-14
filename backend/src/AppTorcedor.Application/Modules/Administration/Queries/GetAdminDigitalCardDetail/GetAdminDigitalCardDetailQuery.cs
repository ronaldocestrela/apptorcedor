using AppTorcedor.Application.Abstractions;
using MediatR;

namespace AppTorcedor.Application.Modules.Administration.Queries.GetAdminDigitalCardDetail;

public sealed record GetAdminDigitalCardDetailQuery(Guid DigitalCardId) : IRequest<AdminDigitalCardDetailDto?>;
