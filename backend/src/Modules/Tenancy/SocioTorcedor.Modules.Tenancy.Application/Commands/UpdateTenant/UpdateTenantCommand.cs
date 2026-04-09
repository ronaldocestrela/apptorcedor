using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.UpdateTenant;

public sealed record UpdateTenantCommand(Guid TenantId, string? Name, string? ConnectionString) : ICommand;
