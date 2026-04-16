using MediatR;

namespace AppTorcedor.Application.Modules.Branding.Queries.GetPublicBranding;

public sealed record GetPublicBrandingQuery : IRequest<PublicBrandingDto>;
