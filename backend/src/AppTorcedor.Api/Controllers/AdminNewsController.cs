using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Abstractions;
using AppTorcedor.Application.Modules.Administration.Commands.CreateAdminNews;
using AppTorcedor.Application.Modules.Administration.Commands.CreateNewsInAppNotifications;
using AppTorcedor.Application.Modules.Administration.Commands.PublishAdminNews;
using AppTorcedor.Application.Modules.Administration.Commands.UnpublishAdminNews;
using AppTorcedor.Application.Modules.Administration.Commands.UpdateAdminNews;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminNews;
using AppTorcedor.Application.Modules.Administration.Queries.ListAdminNews;
using AppTorcedor.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/news")]
[Authorize]
public sealed class AdminNewsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<ActionResult<AdminNewsListPageDto>> List(
        [FromQuery] string? search,
        [FromQuery] NewsEditorialStatus? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new ListAdminNewsQuery(search, status, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        return Ok(pageDto);
    }

    [HttpGet("{newsId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<ActionResult<AdminNewsDetailDto>> GetById(Guid newsId, CancellationToken cancellationToken)
    {
        var detail = await mediator.Send(new GetAdminNewsQuery(newsId), cancellationToken).ConfigureAwait(false);
        return detail is null ? NotFound() : Ok(detail);
    }

    [HttpPost]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<ActionResult<object>> Create([FromBody] UpsertNewsRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var dto = Map(body);
        var result = await mediator.Send(new CreateAdminNewsCommand(dto), cancellationToken).ConfigureAwait(false);
        if (!result.Ok)
            return BadRequest(new { error = MapCreateError(result.Error) });

        return CreatedAtAction(nameof(GetById), new { newsId = result.NewsId }, new { newsId = result.NewsId });
    }

    [HttpPut("{newsId:guid}")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<IActionResult> Update(Guid newsId, [FromBody] UpsertNewsRequest body, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(new UpdateAdminNewsCommand(newsId, Map(body)), cancellationToken)
            .ConfigureAwait(false);
        return MapNewsMutation(result);
    }

    [HttpPost("{newsId:guid}/publish")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<IActionResult> Publish(Guid newsId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new PublishAdminNewsCommand(newsId), cancellationToken).ConfigureAwait(false);
        return MapNewsMutation(result);
    }

    [HttpPost("{newsId:guid}/unpublish")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<IActionResult> Unpublish(Guid newsId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UnpublishAdminNewsCommand(newsId), cancellationToken).ConfigureAwait(false);
        return MapNewsMutation(result);
    }

    [HttpPost("{newsId:guid}/notifications")]
    [Authorize(Policy = Policies.PermissionPrefix + ApplicationPermissions.NoticiasPublicar)]
    public async Task<IActionResult> CreateNotifications(
        Guid newsId,
        [FromBody] CreateNewsNotificationsRequest body,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
            return ValidationProblem(ModelState);

        var result = await mediator
            .Send(
                new CreateNewsInAppNotificationsCommand(newsId, body.ScheduledAt, body.UserIds),
                cancellationToken)
            .ConfigureAwait(false);

        if (result.Ok)
            return NoContent();

        return result.Error switch
        {
            NewsNotificationError.NotFound => NotFound(),
            NewsNotificationError.InvalidState => BadRequest(new { error = "News must be published to notify." }),
            NewsNotificationError.Validation => BadRequest(new { error = "Invalid notification targets." }),
            _ => BadRequest(),
        };
    }

    private static AdminNewsWriteDto Map(UpsertNewsRequest body) =>
        new(body.Title, body.Summary, body.Content);

    private static string MapCreateError(NewsMutationError? error) =>
        error switch
        {
            NewsMutationError.Validation => "Validation failed.",
            _ => "Unable to create news.",
        };

    private IActionResult MapNewsMutation(NewsMutationResult result)
    {
        if (result.Ok)
            return NoContent();
        return result.Error switch
        {
            NewsMutationError.NotFound => NotFound(),
            NewsMutationError.Validation => BadRequest(new { error = "Validation failed." }),
            NewsMutationError.InvalidState => BadRequest(new { error = "Invalid state transition." }),
            _ => BadRequest(),
        };
    }
}
