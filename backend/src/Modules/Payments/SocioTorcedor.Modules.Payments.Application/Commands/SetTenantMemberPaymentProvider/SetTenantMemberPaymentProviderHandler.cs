using SocioTorcedor.BuildingBlocks.Application.Abstractions;
using SocioTorcedor.BuildingBlocks.Shared.Results;
using SocioTorcedor.Modules.Payments.Application.Contracts;
using SocioTorcedor.Modules.Payments.Domain.Entities;

namespace SocioTorcedor.Modules.Payments.Application.Commands.SetTenantMemberPaymentProvider;

public sealed class SetTenantMemberPaymentProviderHandler(ITenantMasterPaymentsRepository masterPaymentsRepository)
    : ICommandHandler<SetTenantMemberPaymentProviderCommand>
{
    public async Task<Result> Handle(SetTenantMemberPaymentProviderCommand command, CancellationToken cancellationToken)
    {
        var cfg = await masterPaymentsRepository.GetMemberGatewayConfigurationByTenantIdAsync(command.TenantId, cancellationToken);
        if (cfg is null)
        {
            cfg = TenantMemberGatewayConfiguration.Create(command.TenantId, command.Provider);
            await masterPaymentsRepository.AddMemberGatewayConfigurationAsync(cfg, cancellationToken);
        }
        else
        {
            cfg.SetSelectedProvider(command.Provider);
        }

        await masterPaymentsRepository.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
