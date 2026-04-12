using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.Modules.Payments.Application.DTOs;

namespace SocioTorcedor.Modules.Payments.Application.Commands.SyncStripeConnectStatus;

public sealed record SyncStripeConnectStatusCommand(Guid TenantId) : ICommand<StripeConnectStatusDto>;
