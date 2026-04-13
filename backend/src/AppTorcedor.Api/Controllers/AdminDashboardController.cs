using AppTorcedor.Api.Authorization;
using AppTorcedor.Api.Contracts;
using AppTorcedor.Application.Modules.Administration.Queries.GetAdminDashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AppTorcedor.Api.Controllers;

[ApiController]
[Route("api/admin/dashboard")]
[Authorize]
public sealed class AdminDashboardController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [Authorize(Policy = Policies.AdminDashboard)]
    public async Task<ActionResult<AdminDashboardResponse>> Get(CancellationToken cancellationToken)
    {
        var dto = await mediator.Send(new GetAdminDashboardQuery(), cancellationToken).ConfigureAwait(false);
        return Ok(new AdminDashboardResponse(dto.ActiveMembersCount, dto.DelinquentMembersCount, dto.OpenSupportTickets));
    }
}
