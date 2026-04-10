using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Identity.Application.Commands.PublishLegalDocumentVersion;
using SocioTorcedor.Modules.Identity.Application.Queries.GetCurrentLegalDocuments;
using SocioTorcedor.Modules.Identity.Domain.Enums;

namespace SocioTorcedor.Modules.Identity.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class LegalDocumentsController(IMediator mediator) : ControllerBase
{
    public sealed class PublishLegalDocumentBody
    {
        public LegalDocumentKind Kind { get; set; }

        public string Content { get; set; } = string.Empty;
    }

    [AllowAnonymous]
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetCurrentLegalDocumentsQuery(), cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error!);

        return Ok(result.Value);
    }

    [Authorize(Roles = "Administrador")]
    [HttpPost]
    public async Task<IActionResult> Publish([FromBody] PublishLegalDocumentBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new PublishLegalDocumentVersionCommand(body.Kind, body.Content),
            cancellationToken);
        if (!result.IsSuccess)
            return MapError(result.Error!);

        return NoContent();
    }

    private static IActionResult MapError(Error error) =>
        error.Code switch
        {
            "Tenant.Required" => new BadRequestObjectResult(new { code = error.Code, message = error.Message }),
            "Identity.LegalDocumentsNotConfigured" => new NotFoundObjectResult(new { code = error.Code, message = error.Message }),
            "Identity.InvalidLegalDocument" => new BadRequestObjectResult(new { code = error.Code, message = error.Message }),
            _ => new BadRequestObjectResult(new { code = error.Code, message = error.Message })
        };
}
