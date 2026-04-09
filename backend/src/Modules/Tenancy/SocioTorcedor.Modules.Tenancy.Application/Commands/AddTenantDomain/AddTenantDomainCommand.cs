using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.AddTenantDomain;

public sealed record AddTenantDomainCommand(Guid TenantId, string Origin) : ICommand<Guid>;
