using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.BuildingBlocks.Shared.Results;

namespace SocioTorcedor.Modules.Backoffice.Api.Controllers;

public abstract class BackofficeControllerBase : ControllerBase
{
    protected IActionResult FromResult(Result result)
    {
        if (result.IsSuccess)
            return Ok();

        return ProblemResult(result.Error!);
    }

    protected IActionResult FromResult<T>(Result<T> result, Func<T, IActionResult> onSuccess)
    {
        if (result.IsSuccess)
            return onSuccess(result.Value!);

        return ProblemResult(result.Error!);
    }

    protected IActionResult ProblemResult(Error error)
    {
        if (error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            return NotFound(new { code = error.Code, message = error.Message });

        if (error.Code.Contains("Conflict", StringComparison.OrdinalIgnoreCase) ||
            error.Code.Contains("Exists", StringComparison.OrdinalIgnoreCase))
            return Conflict(new { code = error.Code, message = error.Message });

        return BadRequest(new { code = error.Code, message = error.Message });
    }
}
