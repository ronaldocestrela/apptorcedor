using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.CreateTenant;

public sealed record CreateTenantCommand(string Name, string Slug) : ICommand<Guid>;
