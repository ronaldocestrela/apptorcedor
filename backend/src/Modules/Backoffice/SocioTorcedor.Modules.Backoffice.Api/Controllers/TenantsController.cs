using MediatR;
using Microsoft.AspNetCore.Mvc;
using SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantDomain;
using SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantSetting;
using SocioTorcedor.Modules.Tenancy.Application.Commands.ChangeTenantStatus;
using SocioTorcedor.Modules.Tenancy.Application.Commands.CreateTenant;
using SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantDomain;
using SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantSetting;
using SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenant;
using SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenantSetting;
using SocioTorcedor.Modules.Tenancy.Application.Queries.GetTenantById;
using SocioTorcedor.Modules.Tenancy.Application.Queries.ListTenants;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;

namespace SocioTorcedor.Modules.Backoffice.Api.Controllers;

[ApiController]
[Route("api/backoffice/tenants")]
public sealed class TenantsController(IMediator mediator) : BackofficeControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new CreateTenantCommand(body.Name, body.Slug),
            cancellationToken);

        return FromResult(result, id => CreatedAtAction(nameof(GetById), new { id }, new { id }));
    }

    [HttpGet]
    public async Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] TenantStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        var result = await mediator.Send(new ListTenantsQuery(page, pageSize, search, status), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new GetTenantByIdQuery(id), cancellationToken);
        return FromResult(result, Ok);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateTenantBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(
            new UpdateTenantCommand(id, body.Name, body.ConnectionString),
            cancellationToken);

        return FromResult(result);
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] ChangeStatusBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new ChangeTenantStatusCommand(id, body.Status), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/domains")]
    public async Task<IActionResult> AddDomain(Guid id, [FromBody] AddDomainBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddTenantDomainCommand(id, body.Origin), cancellationToken);
        return FromResult(result, domainId => CreatedAtAction(nameof(GetById), new { id }, new { domainId }));
    }

    [HttpDelete("{id:guid}/domains/{domainId:guid}")]
    public async Task<IActionResult> RemoveDomain(Guid id, Guid domainId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveTenantDomainCommand(id, domainId), cancellationToken);
        return FromResult(result);
    }

    [HttpPost("{id:guid}/settings")]
    public async Task<IActionResult> AddSetting(Guid id, [FromBody] AddSettingBody body, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new AddTenantSettingCommand(id, body.Key, body.Value), cancellationToken);
        return FromResult(result, settingId => CreatedAtAction(nameof(GetById), new { id }, new { settingId }));
    }

    [HttpPut("{id:guid}/settings/{settingId:guid}")]
    public async Task<IActionResult> UpdateSetting(
        Guid id,
        Guid settingId,
        [FromBody] UpdateSettingBody body,
        CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new UpdateTenantSettingCommand(id, settingId, body.Value), cancellationToken);
        return FromResult(result);
    }

    [HttpDelete("{id:guid}/settings/{settingId:guid}")]
    public async Task<IActionResult> RemoveSetting(Guid id, Guid settingId, CancellationToken cancellationToken)
    {
        var result = await mediator.Send(new RemoveTenantSettingCommand(id, settingId), cancellationToken);
        return FromResult(result);
    }

    public sealed class CreateTenantBody
    {
        public string Name { get; set; } = string.Empty;

        public string Slug { get; set; } = string.Empty;
    }

    public sealed class UpdateTenantBody
    {
        public string? Name { get; set; }

        public string? ConnectionString { get; set; }
    }

    public sealed class ChangeStatusBody
    {
        public TenantStatus Status { get; set; }
    }

    public sealed class AddDomainBody
    {
        public string Origin { get; set; } = string.Empty;
    }

    public sealed class AddSettingBody
    {
        public string Key { get; set; } = string.Empty;

        public string Value { get; set; } = string.Empty;
    }

    public sealed class UpdateSettingBody
    {
        public string Value { get; set; } = string.Empty;
    }
}
