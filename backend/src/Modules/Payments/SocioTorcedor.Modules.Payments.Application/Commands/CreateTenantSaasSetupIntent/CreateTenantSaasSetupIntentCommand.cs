using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.CreateTenantSaasSetupIntent;

public sealed record CreateTenantSaasSetupIntentCommand(Guid TenantId) : ICommand<TenantSaasSetupIntentDto>;
