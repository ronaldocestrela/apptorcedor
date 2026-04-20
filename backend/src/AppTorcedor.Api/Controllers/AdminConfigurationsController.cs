using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Administration.Commands.UpsertAppConfiguration;
using AppTorcedor.Application.Modules.Administration.Queries.ListAppConfigurations;
using AppTorcedor.Application.Modules.Branding.Commands.UploadTeamShield;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/config")]
[Authorize]
public sealed class AdminConfigurationsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ConfiguracoesVisualizar)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var rows = await mediator.Send(new ListAppConfigurationsQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPut("{key}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ConfiguracoesEditar)]
    public async Task<IActionResult> Upsert(string key, [FromBody] UpsertAppConfigurationRequest body, CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new UpsertAppConfigurationCommand(key, body.Value), cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }

    [HttpPost("team-shield")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.ConfiguracoesEditar)]
    [RequestSizeLimit(6 * 1024 * 1024)]
    public async Task<ActionResult<TeamShieldUploadResponse>> UploadTeamShield(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
            return BadRequest();

        await using var stream = file.OpenReadStream();
        var result = await mediator
            .Send(new UploadTeamShieldCommand(stream, file.FileName, file.ContentType), cancellationToken)
            .ConfigureAwait(false);
        if (result is null)
            return BadRequest();

        return Ok(new TeamShieldUploadResponse { TeamShieldUrl = result.TeamShieldUrl });
    }
}
