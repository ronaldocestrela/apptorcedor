using SocioTorcedor.BuildingBlocks.Application.Abstractions;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.RemoveTenantDomain;

public sealed record RemoveTenantDomainCommand(Guid TenantId, Guid DomainId) : ICommand;
