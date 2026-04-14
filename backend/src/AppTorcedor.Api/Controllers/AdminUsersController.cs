using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Users.Commands.SetUserAccountActive;
using AppTorcedor.Application.Modules.Users.Commands.UpsertAdminUserProfile;
using AppTorcedor.Application.Modules.Users.Queries.GetAdminUserDetail;
using AppTorcedor.Application.Modules.Users.Queries.ListAdminUsers;
using AppTorcedor.Application.Modules.Users.Queries.ListUserAuditLogs;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/users")]
[Authorize]
public sealed class AdminUsersController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosVisualizar)]
    public async Task<IActionResult> List(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator
            .Send(new ListAdminUsersQuery(search, isActive, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }

    [HttpGet("{userId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosVisualizar)]
    public async Task<IActionResult> Get(Guid userId, CancellationToken cancellationToken = default)
    {
        var detail = await mediator.Send(new GetAdminUserDetailQuery(userId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpGet("{userId:guid}/audit-logs")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosVisualizar)]
    public async Task<IActionResult> ListAuditLogs(
        Guid userId,
        [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        var rows = await mediator
            .Send(new ListUserAuditLogsForUserQuery(userId, take), cancellationToken)
            .ConfigureAwait(false);
        return Ok(rows);
    }

    [HttpPatch("{userId:guid}/active")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosEditar)]
    public async Task<IActionResult> SetActive(
        Guid userId,
        [FromBody] SetUserAccountActiveRequest body,
        CancellationToken cancellationToken = default)
    {
        var ok = await mediator
            .Send(new SetUserAccountActiveCommand(userId, body.IsActive), cancellationToken)
            .ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }

    [HttpPut("{userId:guid}/profile")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.UsuariosEditar)]
    public async Task<IActionResult> UpsertProfile(
        Guid userId,
        [FromBody] UpsertAdminUserProfileRequest body,
        CancellationToken cancellationToken = default)
    {
        var patch = new AdminUserProfileUpsertDto(
            body.Document,
            body.BirthDate,
            body.PhotoUrl,
            body.Address,
            body.AdministrativeNote);
        var ok = await mediator
            .Send(new UpsertAdminUserProfileCommand(userId, patch), cancellationToken)
            .ConfigureAwait(false);
        return ok ? NoContent() : NotFound();
    }
}
