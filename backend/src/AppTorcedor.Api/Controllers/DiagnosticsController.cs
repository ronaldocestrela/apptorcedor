using AppTorcedor.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/diagnostics")]
public sealed class DiagnosticsController : ControllerBase
{
    [HttpGet("admin-master-only")]
    [Authorize(Roles = SystemRoles.AdministradorMaster)]
    public IActionResult AdminMasterOnly() => Ok(new { ok = true });
}
