using AppTorcedor.Api.Authorization;
using AppTorcedor.Application.Modules.Administration.Queries.GetDiagnostics;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController(IMediator mediator) : ControllerBase
{
    [HttpGet("admin-master-only")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.AdministracaoDiagnostics)]
    public async Task<IActionResult> AdminMasterOnly(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetDiagnosticsQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(result);
    }
}
