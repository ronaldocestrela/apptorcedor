using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Branding.Queries.GetPublicBranding;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/branding")]
public sealed class BrandingController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<PublicBrandingResponse>> Get(CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetPublicBrandingQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(new PublicBrandingResponse { TeamShieldUrl = dto.TeamShieldUrl });
    }
}
