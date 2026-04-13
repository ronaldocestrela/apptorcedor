using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetNewsFeed;
using AppTorcedor.Application.Modules.Torcedor.Queries.GetPublishedNewsDetail;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/news")]
[Authorize]
public sealed class TorcedorNewsController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<TorcedorNewsFeedPageResponse>> List(
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var pageDto = await mediator
            .Send(new GetNewsFeedQuery(search, page, pageSize), cancellationToken)
            .ConfigureAwait(false);
        var items = pageDto.Items
            .Select(i => new TorcedorNewsFeedItemResponse(
                i.NewsId,
                i.Title,
                i.Summary,
                i.PublishedAt,
                i.UpdatedAt))
            .ToList();
        return Ok(new TorcedorNewsFeedPageResponse(pageDto.TotalCount, items));
    }

    [HttpGet("{newsId:guid}")]
    public async Task<ActionResult<TorcedorNewsDetailResponse>> GetById(Guid newsId, CancellationToken cancellationToken)
    {
        var d = await mediator.Send(new GetPublishedNewsDetailQuery(newsId), cancellationToken).ConfigureAwait(false);
        if (d is null)
            return NotFound();
        return Ok(
            new TorcedorNewsDetailResponse(
                d.NewsId,
                d.Title,
                d.Summary,
                d.Content,
                d.PublishedAt,
                d.UpdatedAt));
    }
}
