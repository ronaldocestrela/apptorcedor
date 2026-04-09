using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Tenancy.Domain.Enums;

namespace SocioTorcedor.Modules.Tenancy.Application.Commands.ChangeTenantStatus;

public sealed record ChangeTenantStatusCommand(Guid TenantId, TenantStatus Status) : ICommand;
