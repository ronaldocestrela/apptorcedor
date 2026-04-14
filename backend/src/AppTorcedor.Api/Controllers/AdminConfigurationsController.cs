using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Administration.Commands.UpsertAppConfiguration;
using AppTorcedor.Application.Modules.Administration.Queries.ListAppConfigurations;
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
}
